import os
from jira import JIRA

# Jira server and authentication
JIRA_SERVER = os.getenv("JIRA_SERVER")
JIRA_USER = os.getenv("JIRA_USER")
JIRA_API_TOKEN = os.getenv("JIRA_API_TOKEN")

# Connect to Jira
jira = JIRA(server=JIRA_SERVER, basic_auth=(JIRA_USER, JIRA_API_TOKEN))

# Define JQL query to fetch tickets in a certain state
# Example: Fetch tickets in "To Do" state in a specific project
PROJECT_KEY = "PROJECT_KEY"
STATUS = "To Do"
jql_query = f'project = {PROJECT_KEY} AND status = "{STATUS}"'

# Fetch tickets
tickets = jira.search_issues(jql_query)

# Print ticket details
for ticket in tickets:
    print(f"Key: {ticket.key}, Summary: {ticket.fields.summary}, Status: {ticket.fields.status.name}")