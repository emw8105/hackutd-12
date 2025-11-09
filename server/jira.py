import os
import logging
from typing import Optional, List, Dict, Any
from dotenv import load_dotenv
from atlassian import Jira
import requests
import base64

load_dotenv()

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class JiraClient:
    def __init__(self):
        """Initialize the Jira client with credentials from environment variables."""
        self.jira_url = os.getenv("JIRA_URL")
        self.username = os.getenv("JIRA_USERNAME")
        self.api_token = os.getenv("JIRA_API_TOKEN")
        self.project_key = os.getenv("JIRA_PROJECT_KEY", "")

        if not all([self.jira_url, self.username, self.api_token]):
            raise ValueError(
                "Missing required environment variables. Please set JIRA_URL, JIRA_USERNAME, and JIRA_API_TOKEN"
            )

        # Initialize Jira client
        try:
            self.jira = Jira(
                url=self.jira_url,
                username=self.username,
                password=self.api_token,
                cloud=True
            )
            logger.info(f"Successfully connected to Jira at {self.jira_url}")
        except Exception as e:
            logger.error(f"Failed to connect to Jira: {e}")
            raise

        # Store tickets in memory
        self.tickets: List[Dict[str, Any]] = []

    def get_all_tickets(self, project_key: Optional[str] = None, max_results: int = 1000) -> List[Dict[str, Any]]:
        """
        Pull all tickets from Jira regardless of completion status.

        Args:
            project_key: Optional project key to filter tickets. Uses default if not provided.
            max_results: Maximum number of results to return (default: 1000)

        Returns:
            List of ticket dictionaries with key information
        """
        try:
            # Use provided project key or default
            proj_key = project_key or self.project_key

            # Build JQL query - project key is required for Jira Cloud
            if proj_key:
                jql = f'project = {proj_key} ORDER BY created DESC'
            else:
                try:
                    projects = self.jira.projects(included_archived=None)
                    if not projects:
                        logger.warning(
                            "No projects found. Cannot fetch tickets.")
                        self.tickets = []
                        return self.tickets

                    project_keys = [p['key'] for p in projects]
                    jql = f'project in ({",".join(project_keys)}) ORDER BY created DESC'
                    logger.info(
                        f"No project key specified, fetching from all accessible projects: {project_keys}")
                except Exception as e:
                    logger.error(f"Failed to get projects list: {e}")
                    logger.info(
                        "Please set JIRA_PROJECT_KEY in your .env file")
                    self.tickets = []
                    return self.tickets

            logger.info(f"Fetching tickets with JQL: {jql}")

            # Fetch issues using JQL
            issues = self.jira.jql(jql, limit=max_results)

            # Parse and store tickets
            self.tickets = []
            for issue in issues.get('issues', []):
                ticket = self._parse_ticket(issue)
                self.tickets.append(ticket)

            logger.info(f"Successfully fetched {len(self.tickets)} tickets")
            return self.tickets

        except Exception as e:
            logger.error(f"Failed to fetch tickets: {e}")
            raise

    def _parse_ticket(self, issue: Dict[str, Any]) -> Dict[str, Any]:
        """
        Parse raw Jira issue into a simplified ticket structure.

        Args:
            issue: Raw issue data from Jira API

        Returns:
            Parsed ticket dictionary
        """
        fields = issue.get('fields', {})

        return {
            'key': issue.get('key'),
            'id': issue.get('id'),
            'summary': fields.get('summary'),
            'description': fields.get('description'),
            'status': fields.get('status', {}).get('name'),
            'status_id': fields.get('status', {}).get('id'),
            'priority': fields.get('priority', {}).get('name') if fields.get('priority') else None,
            'assignee': fields.get('assignee', {}).get('displayName') if fields.get('assignee') else None,
            'reporter': fields.get('reporter', {}).get('displayName') if fields.get('reporter') else None,
            'created': fields.get('created'),
            'updated': fields.get('updated'),
            'project': fields.get('project', {}).get('key'),
            'issue_type': fields.get('issuetype', {}).get('name'),
            'labels': fields.get('labels', []),
        }

    def update_ticket_status(self, ticket_key: str, status_name: str) -> Dict[str, Any]:
        """
        Update the status of a Jira ticket.

        Args:
            ticket_key: The ticket key (e.g., 'PROJ-123')
            status_name: The target status name (e.g., 'In Progress', 'Done')

        Returns:
            Updated ticket information
        """
        try:
            # Get available transitions for this issue
            transitions = self.jira.get_issue_transitions(ticket_key)

            # Find the transition ID that matches the desired status
            transition_id = None
            for transition in transitions:
                if transition['name'].lower() == status_name.lower() or \
                   transition['to'].lower() == status_name.lower():
                    transition_id = transition['id']
                    break

            if not transition_id:
                print("Transitions: ", transitions)
                available_statuses = [t['to'] for t in transitions]
                raise ValueError(
                    f"Status '{status_name}' not found. Available transitions: {available_statuses}"
                )

            # Execute the transition
            self.jira.set_issue_status(ticket_key, status_name)

            logger.info(
                f"Successfully updated {ticket_key} to status '{status_name}'")

            # Fetch and return updated ticket
            issue = self.jira.issue(ticket_key)
            return self._parse_ticket(issue)

        except Exception as e:
            logger.error(f"Failed to update ticket status: {e}")
            raise

    def add_comment(self, ticket_key: str, comment_text: str) -> Dict[str, Any]:
        """
        Add a comment to a Jira ticket.

        Args:
            ticket_key: The ticket key (e.g., 'PROJ-123')
            comment_text: The comment text to add

        Returns:
            The created comment data
        """
        try:
            result = self.jira.issue_add_comment(ticket_key, comment_text)
            logger.info(f"Successfully added comment to {ticket_key}")
            return result

        except Exception as e:
            logger.error(f"Failed to add comment: {e}")
            raise

    def add_attachment(self, ticket_key: str, file_path: str) -> Dict[str, Any]:
        """
        Add an attachment (image or file) to a Jira ticket.

        Args:
            ticket_key: The ticket key (e.g., 'PROJ-123')
            file_path: Path to the file to attach

        Returns:
            The attachment information
        """
        try:
            # Read the file
            with open(file_path, 'rb') as f:
                result = self.jira.add_attachment(
                    ticket_key, f, file_path.split('/')[-1])

            logger.info(f"Successfully added attachment to {ticket_key}")
            return result

        except Exception as e:
            logger.error(f"Failed to add attachment: {e}")
            raise

    def add_attachment_from_bytes(self, ticket_key: str, file_data: bytes, filename: str) -> Dict[str, Any]:
        """
        Add an attachment from bytes data (useful for uploaded files).

        Args:
            ticket_key: The ticket key (e.g., 'PROJ-123')
            file_data: Binary file data
            filename: Name for the attachment

        Returns:
            The attachment information
        """
        try:
            # Construct the API endpoint
            url = f"{self.jira_url}/rest/api/3/issue/{ticket_key}/attachments"

            # Prepare headers
            auth = base64.b64encode(
                f"{self.username}:{self.api_token}".encode()).decode()
            headers = {
                "Authorization": f"Basic {auth}",
                "X-Atlassian-Token": "no-check"
            }

            # Prepare files
            files = {'file': (filename, file_data)}

            # Make the request
            response = requests.post(url, headers=headers, files=files)
            response.raise_for_status()

            logger.info(
                f"Successfully added attachment '{filename}' to {ticket_key}")
            return response.json()

        except Exception as e:
            logger.error(f"Failed to add attachment from bytes: {e}")
            raise

    def register_webhook(self, webhook_url: str, events: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        Register a webhook to listen for Jira events.

        Args:
            webhook_url: The URL where Jira should send webhook events
            events: List of events to listen for. If None, uses default events.
                   Common events: 'jira:issue_created', 'jira:issue_updated',
                   'jira:issue_deleted', 'comment_created', 'comment_updated'

        Returns:
            The created webhook information
        """
        try:
            # Default events if none provided
            if events is None:
                events = [
                    'jira:issue_created',
                    'jira:issue_updated',
                    'jira:issue_deleted',
                    'comment_created',
                    'comment_updated'
                ]

            # Construct the webhook data
            webhook_data = {
                "name": "Auto-generated webhook",
                "url": webhook_url,
                "events": events,
                "filters": {},
                "excludeBody": False
            }

            # Jira Cloud uses different webhook registration
            # For Jira Cloud, webhooks are managed through the app settings
            # This uses the REST API v3
            url = f"{self.jira_url}/rest/webhooks/1.0/webhook"

            auth = base64.b64encode(
                f"{self.username}:{self.api_token}".encode()).decode()
            headers = {
                "Authorization": f"Basic {auth}",
                "Content-Type": "application/json",
                "Accept": "application/json"
            }

            response = requests.post(url, json=webhook_data, headers=headers)
            response.raise_for_status()

            result = response.json()
            logger.info(f"Successfully registered webhook at {webhook_url}")
            return result

        except Exception as e:
            logger.error(f"Failed to register webhook: {e}")
            logger.info(
                "Note: Webhook registration might require admin permissions. "
                "You may need to register webhooks manually through Jira Settings > System > Webhooks"
            )
            raise

    def list_webhooks(self) -> List[Dict[str, Any]]:
        """
        List all registered webhooks.

        Returns:
            List of webhook configurations
        """
        try:
            url = f"{self.jira_url}/rest/webhooks/1.0/webhook"

            auth = base64.b64encode(
                f"{self.username}:{self.api_token}".encode()).decode()
            headers = {
                "Authorization": f"Basic {auth}",
                "Accept": "application/json"
            }

            response = requests.get(url, headers=headers)
            response.raise_for_status()

            webhooks = response.json()
            logger.info(f"Found {len(webhooks)} registered webhooks")
            return webhooks

        except Exception as e:
            logger.error(f"Failed to list webhooks: {e}")
            raise

    def delete_webhook(self, webhook_id: str) -> bool:
        """
        Delete a registered webhook.

        Args:
            webhook_id: The ID of the webhook to delete

        Returns:
            True if successful
        """
        try:
            url = f"{self.jira_url}/rest/webhooks/1.0/webhook/{webhook_id}"

            auth = base64.b64encode(
                f"{self.username}:{self.api_token}".encode()).decode()
            headers = {
                "Authorization": f"Basic {auth}",
            }

            response = requests.delete(url, headers=headers)
            response.raise_for_status()

            logger.info(f"Successfully deleted webhook {webhook_id}")
            return True

        except Exception as e:
            logger.error(f"Failed to delete webhook: {e}")
            raise

    def get_ticket_by_key(self, ticket_key: str) -> Dict[str, Any]:
        """
        Get a specific ticket by its key.

        Args:
            ticket_key: The ticket key (e.g., 'PROJ-123')

        Returns:
            Ticket information
        """
        try:
            issue = self.jira.issue(ticket_key)
            return self._parse_ticket(issue)

        except Exception as e:
            logger.error(f"Failed to get ticket {ticket_key}: {e}")
            raise

    def get_available_transitions(self, ticket_key: str) -> List[Dict[str, Any]]:
        """
        Get available status transitions for a ticket.

        Args:
            ticket_key: The ticket key (e.g., 'PROJ-123')

        Returns:
            List of available transitions
        """
        try:
            transitions = self.jira.get_issue_transitions(ticket_key)
            return transitions.get('transitions', [])

        except Exception as e:
            logger.error(f"Failed to get transitions for {ticket_key}: {e}")
            raise


# Initialize global Jira client instance
jira_client: Optional[JiraClient] = None


def initialize_jira_client() -> JiraClient:
    """
    Initialize the global Jira client and fetch all tickets on startup.

    Returns:
        The initialized JiraClient instance
    """
    global jira_client

    try:
        logger.info("Initializing Jira client...")
        jira_client = JiraClient()

        # Pull all tickets on startup
        logger.info("Fetching all tickets on startup...")
        tickets = jira_client.get_all_tickets()
        logger.info(f"Startup complete. Loaded {len(tickets)} tickets.")

        return jira_client

    except Exception as e:
        logger.error(f"Failed to initialize Jira client: {e}")
        raise


def get_jira_client() -> JiraClient:
    """
    Get the global Jira client instance.

    Returns:
        The JiraClient instance

    Raises:
        RuntimeError if the client hasn't been initialized
    """
    if jira_client is None:
        raise RuntimeError(
            "Jira client not initialized. Call initialize_jira_client() first.")
    return jira_client
