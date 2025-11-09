from fastapi import FastAPI, HTTPException, File, UploadFile, WebSocket
from contextlib import asynccontextmanager
from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.interval import IntervalTrigger
import logging
import json

from task_assignment import task_assigner
from models import (
    JiraTicketListResponse,
    JiraTicket,
    JiraStatusUpdate,
    JiraComment,
    Technician,
    Location,
    Server,
    TechnicianEvents,
    TechnicianResponse
)
from jira import initialize_jira_client, get_jira_client
from technician_store import initialize_technician_store, get_technician_store
from server_store import initialize_server_store, get_server_store
from get_server_data import get_server_metrics, get_server_logs

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
        jira_tickets = [JiraTicket(**ticket) for ticket in tickets]
        task_assigner.refresh_tasks(jira_tickets)
        logger.info(f"Successfully refreshed {len(tickets)} tickets")
    except Exception as e:
        logger.error(f"Failed to refresh tickets in scheduled task: {e}")


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup: Initialize Jira client, technician store, and fetch all tickets
    try:
        initialize_jira_client()
        refresh_jira_tickets()

        # Start the scheduler
        scheduler.add_job(
            refresh_jira_tickets,
            trigger=IntervalTrigger(minutes=15),
            id="refresh_jira_tickets",
            name="Refresh Jira tickets every 15 minutes",
            replace_existing=True,
        )
        scheduler.start()
        logger.info("Scheduler started - tickets will refresh every 15 minutes")

    except Exception as e:
        print(f"Warning: Failed to initialize Jira client: {e}")
        print("Server will start but Jira endpoints will not work.")

    # Initialize technician store
    initialize_technician_store()
    logger.info("Technician store initialized")

    # Initialize server store with JSON data
    try:
        initialize_server_store("server_locations.json")
        logger.info(
            "Server store initialized with data from server_locations.json")
    except Exception as e:
        logger.error(f"Failed to initialize server store: {e}")
        print(f"Warning: Failed to initialize server store: {e}")

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
        return JiraTicketListResponse(tickets=tickets, count=len(tickets))
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


@app.get("/items/by-server/{server_id}")
def get_tickets_by_server(server_id: str):
    """Get all Jira tickets associated with a specific server ID."""
    try:
        client = get_jira_client()
        tickets_data = client.get_tickets_by_server_id(server_id)
        tickets = [JiraTicket(**ticket) for ticket in tickets_data]
        return {
            "tickets": tickets,
            "count": len(tickets),
            "server_id": server_id,
            "message": f"Successfully retrieved tickets for server {server_id}",
        }
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to get tickets for server: {str(e)}"
        )


@app.get("/items/{ticket_key}/server")
def get_server_from_ticket(ticket_key: str):
    """Get the server associated with a specific ticket."""
    try:
        jira_client = get_jira_client()
        ticket = jira_client.get_ticket_by_key(ticket_key)

        server_id = ticket.get("server_id")
        if not server_id:
            raise HTTPException(
                status_code=404,
                detail=f"Ticket {ticket_key} does not have an associated server ID",
            )

        server_store = get_server_store()
        server = server_store.get_server(server_id)

        if not server:
            raise HTTPException(
                status_code=404, detail=f"Server {server_id} not found in server store"
            )

        return {
            "ticket_key": ticket_key,
            "server": server,
            "message": f"Successfully retrieved server for ticket {ticket_key}",
        }
    except HTTPException:
        raise
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=404, detail=f"Ticket not found: {str(e)}")


@app.get("/items/by-server/{server_id}")
def get_tickets_by_server(server_id: str):
    """Get all Jira tickets associated with a specific server ID."""
    try:
        client = get_jira_client()
        tickets_data = client.get_tickets_by_server_id(server_id)
        tickets = [JiraTicket(**ticket) for ticket in tickets_data]
        return {
            "tickets": tickets,
            "count": len(tickets),
            "server_id": server_id,
            "message": f"Successfully retrieved tickets for server {server_id}",
        }
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to get tickets for server: {str(e)}"
        )


@app.get("/items/{ticket_key}/server")
def get_server_from_ticket(ticket_key: str):
    """Get the server associated with a specific ticket."""
    try:
        jira_client = get_jira_client()
        ticket = jira_client.get_ticket_by_key(ticket_key)

        server_id = ticket.get("server_id")
        if not server_id:
            raise HTTPException(
                status_code=404,
                detail=f"Ticket {ticket_key} does not have an associated server ID",
            )

        server_store = get_server_store()
        server = server_store.get_server(server_id)

        if not server:
            raise HTTPException(
                status_code=404, detail=f"Server {server_id} not found in server store"
            )

        return {
            "ticket_key": ticket_key,
            "server": server,
            "message": f"Successfully retrieved server for ticket {ticket_key}",
        }
    except HTTPException:
        raise
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to get server from ticket: {str(e)}"
        )


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
            "ticket": JiraTicket(**updated_ticket),
        }
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to update status: {str(e)}"
        )


@app.post("/items/{ticket_key}/comments")
def add_comment(ticket_key: str, comment: JiraComment):
    """Add a comment to a Jira ticket."""
    try:
        client = get_jira_client()
        result = client.add_comment(ticket_key, comment.comment)

        refresh_jira_tickets()

        return {
            "message": f"Successfully added comment to {ticket_key}",
            "comment": result,
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
            "attachment": result,
        }
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to add attachment: {str(e)}"
        )


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
            message="Successfully refreshed Jira tickets manually",
        )
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to refresh tickets: {str(e)}"
        )


def handle_technician_events(event: TechnicianEvents):
    if event._type == "online":
        task_assigner._refresh_technicians()


# Technician endpoints
@app.post("/technicians", response_model=Technician)
def add_technician(technician: Technician):
    """Add or update a technician in the store."""
    try:
        store = get_technician_store()
        return store.add_technician(technician)
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to add technician: {str(e)}"
        )


@app.get("/technicians")
def get_all_technicians():
    """Get all active technicians."""
    try:
        store = get_technician_store()
        technicians = store.get_all_technicians()
        return {
            "technicians": technicians,
            "count": len(technicians),
            "message": "Successfully retrieved all technicians",
        }
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to get technicians: {str(e)}"
        )


@app.get("/technicians/{technician_id}", response_model=Technician)
def get_technician(technician_id: str):
    """Get a specific technician by ID."""
    try:
        store = get_technician_store()
        technician = store.get_technician(technician_id)
        if technician is None:
            raise HTTPException(
                status_code=404, detail=f"Technician {technician_id} not found"
            )
        return technician
    except HTTPException:
        raise
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to get technician: {str(e)}"
        )


@app.put("/technicians/{technician_id}/location", response_model=Technician)
def update_technician_location(technician_id: str, location: Location):
    """Update the location of an existing technician."""
    try:
        store = get_technician_store()
        technician = store.update_location(technician_id, location)
        if technician is None:
            raise HTTPException(
                status_code=404, detail=f"Technician {technician_id} not found"
            )
        return technician
    except HTTPException:
        raise
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to update location: {str(e)}"
        )


@app.delete("/technicians/{technician_id}")
def remove_technician(technician_id: str):
    """Remove a technician from the store."""
    try:
        store = get_technician_store()
        success = store.remove_technician(technician_id)
        if not success:
            raise HTTPException(
                status_code=404, detail=f"Technician {technician_id} not found"
            )
        return {
            "message": f"Successfully removed technician {technician_id}",
            "technician_id": technician_id,
        }
    except HTTPException:
        raise
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to remove technician: {str(e)}"
        )


# Server endpoints
@app.post("/servers", response_model=Server)
def add_server(server: Server):
    """Add or update a server in the store."""
    try:
        store = get_server_store()
        return store.add_server(server)
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to add server: {str(e)}")


@app.get("/servers")
def get_all_servers():
    """Get all servers."""
    try:
        store = get_server_store()
        servers = store.get_all_servers()
        return {
            "servers": servers,
            "count": len(servers),
            "message": "Successfully retrieved all servers",
        }
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to get servers: {str(e)}")


@app.get("/servers/{server_id}")
def get_server(server_id: str):
    """Get a specific server by ID with real-time metrics and logs."""
    try:
        store = get_server_store()
        server = store.get_server(server_id)
        if server is None:
            raise HTTPException(
                status_code=404, detail=f"Server {server_id} not found")

        metrics = get_server_metrics(server_id)
        logs = get_server_logs(server_id, limit=5)

        return {
            "server": server,
            "metrics": metrics,
            "recent_logs": logs,
            "message": f"Successfully retrieved server {server_id} with metrics and logs"
        }
    except HTTPException:
        raise
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to get server: {str(e)}")


@app.put("/servers/{server_id}/location", response_model=Server)
def update_server_location(server_id: str, location: Location):
    """Update the location of an existing server."""
    try:
        store = get_server_store()
        server = store.update_location(server_id, location)
        if server is None:
            raise HTTPException(status_code=404, detail=f"Server {server_id} not found")
        return server
    except HTTPException:
        raise
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to update location: {str(e)}"
        )


@app.delete("/servers/{server_id}")
def remove_server(server_id: str):
    """Remove a server from the store."""
    try:
        store = get_server_store()
        success = store.remove_server(server_id)
        if not success:
            raise HTTPException(status_code=404, detail=f"Server {server_id} not found")
        return {
            "message": f"Successfully removed server {server_id}",
            "server_id": server_id,
        }
    except HTTPException:
        raise
    except RuntimeError as e:
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to remove server: {str(e)}"
        )


@app.websocket("/ws/technician")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    while True:
        data = await websocket.receive_json()
        logger.info(data)
        try:
            event = TechnicianEvents(**data)  # Process incoming data as needed
            logger.info(event)
            if event.event_type == "online":
                store = get_technician_store()
                tech_id = event.payload.id
                if store.get_technician(tech_id) is not None:
                    logging.info("Technician already exists, skipping add:", tech_id)
                    continue
                store.add_technician(
                    Technician(id=tech_id, location=event.payload.location)
                )
                # TODO: distance
                task_assigner.add_technician(tech_id, 10)

                assignments = task_assigner.assign_tasks()
                logger.info(assignments)
                if tech_id in assignments:
                    response = TechnicianResponse(event_type="assignment",
                                                  payload = assignments[tech_id])
                    await websocket.send_json(response.dict())
        except Exception as e:
            logger.info("Invalid JSON received, ignoring.", data, e)
        continue
