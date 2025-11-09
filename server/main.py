from fastapi import FastAPI, HTTPException, File, UploadFile
from contextlib import asynccontextmanager
from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.interval import IntervalTrigger
import logging

from models import (
    JiraTicketListResponse, JiraTicket, JiraStatusUpdate, JiraComment
)
from jira import initialize_jira_client, get_jira_client

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize scheduler
scheduler = BackgroundScheduler()


def refresh_jira_tickets():
    """Background task to refresh Jira tickets every 15 minutes."""
    try:
        logger.info("Running scheduled ticket refresh...")
        client = get_jira_client()
        tickets = client.get_all_tickets()
        logger.info(f"Successfully refreshed {len(tickets)} tickets")
    except Exception as e:
        logger.error(f"Failed to refresh tickets in scheduled task: {e}")


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup: Initialize Jira client and fetch all tickets
    try:
        initialize_jira_client()

        # Start the scheduler
        scheduler.add_job(
            refresh_jira_tickets,
            trigger=IntervalTrigger(minutes=15),
            id='refresh_jira_tickets',
            name='Refresh Jira tickets every 15 minutes',
            replace_existing=True
        )
        scheduler.start()
        logger.info("Scheduler started - tickets will refresh every 15 minutes")

    except Exception as e:
        print(f"Warning: Failed to initialize Jira client: {e}")
        print("Server will start but Jira endpoints will not work.")

    yield

    # Shutdown: stop the scheduler
    scheduler.shutdown()
    logger.info("Scheduler stopped")


app = FastAPI(lifespan=lifespan)


# Jira endpoints
@app.get("/items", response_model=JiraTicketListResponse)
def get_all_tickets():
    """Get all Jira tickets that were loaded on startup."""
    try:
        client = get_jira_client()
        tickets = [JiraTicket(**ticket) for ticket in client.tickets]
        return JiraTicketListResponse(
            tickets=tickets,
            count=len(tickets)
        )
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to get tickets: {str(e)}")


@app.get("/items/{ticket_key}", response_model=JiraTicket)
def get_ticket(ticket_key: str):
    """Get a specific Jira ticket by its key."""
    try:
        client = get_jira_client()
        ticket = client.get_ticket_by_key(ticket_key)
        return JiraTicket(**ticket)
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=404, detail=f"Ticket not found: {str(e)}")


@app.put("/items/{ticket_key}/status")
def update_ticket_status(ticket_key: str, status_update: JiraStatusUpdate):
    """Update the status of a Jira ticket."""
    try:
        client = get_jira_client()
        updated_ticket = client.update_ticket_status(
            ticket_key, status_update.status)

        # If successful, refresh the tickets
        refresh_jira_tickets()

        return {
            "message": f"Successfully updated {ticket_key} to status '{status_update.status}'",
            "ticket": JiraTicket(**updated_ticket)
        }
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to update status: {str(e)}")


@app.post("/items/{ticket_key}/comments")
def add_comment(ticket_key: str, comment: JiraComment):
    """Add a comment to a Jira ticket."""
    try:
        client = get_jira_client()
        result = client.add_comment(ticket_key, comment.comment)

        refresh_jira_tickets()

        return {
            "message": f"Successfully added comment to {ticket_key}",
            "comment": result
        }
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to add comment: {str(e)}")


@app.post("/items/{ticket_key}/attachments")
async def add_attachment(ticket_key: str, file: UploadFile = File(...)):
    """Add an attachment (image or file) to a Jira ticket."""
    try:
        client = get_jira_client()

        # Read file data
        file_data = await file.read()

        # Upload to Jira
        result = client.add_attachment_from_bytes(
            ticket_key, file_data, file.filename)

        refresh_jira_tickets()

        return {
            "message": f"Successfully added attachment '{file.filename}' to {ticket_key}",
            "attachment": result
        }
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to add attachment: {str(e)}")


@app.post("/items/refresh-now")
def manual_refresh():
    """Manually trigger a ticket refresh (in addition to the automatic 15-minute schedule)."""
    try:
        logger.info("Manual ticket refresh triggered")
        client = get_jira_client()
        tickets_data = client.get_all_tickets()
        tickets = [JiraTicket(**ticket) for ticket in tickets_data]
        return JiraTicketListResponse(
            tickets=tickets,
            count=len(tickets),
            message="Successfully refreshed Jira tickets manually"
        )
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to refresh tickets: {str(e)}")
