from typing import Dict, List, Optional
import json
import logging
from pathlib import Path
from models import Server, Location

logger = logging.getLogger(__name__)


class ServerStore:
    def __init__(self, json_file_path: Optional[str] = None):
        self._servers: Dict[str, Server] = {}
        if json_file_path:
            self._load_from_json(json_file_path)

    def _load_from_json(self, json_file_path: str) -> None:
        """
        Load servers from a JSON file.

        Args:
            json_file_path: Path to the JSON file containing server data
        """
        try:
            path = Path(json_file_path)
            if not path.exists():
                logger.warning(
                    f"Server locations file not found: {json_file_path}")
                return

            with open(path, 'r') as f:
                data = json.load(f)

            for server_data in data:
                location = Location(**server_data['location'])
                server = Server(
                    id=server_data['id'],
                    name=server_data['name'],
                    location=location
                )
                self._servers[server.id] = server

            logger.info(
                f"Loaded {len(self._servers)} servers from {json_file_path}")
        except Exception as e:
            logger.error(f"Failed to load servers from JSON: {e}")
            raise

    def add_server(self, server: Server) -> Server:
        """
        Add or update a server in the store.

        Args:
            server: Server object with id, name, and location

        Returns:
            The added/updated server
        """
        self._servers[server.id] = server
        logger.info(
            f"Server {server.id} ({server.name}) added/updated at location ({server.location.x}, {server.location.y}, {server.location.z})")
        return server

    def remove_server(self, server_id: str) -> bool:
        """
        Remove a server from the store.

        Args:
            server_id: ID of the server to remove

        Returns:
            True if server was removed, False if not found
        """
        if server_id in self._servers:
            del self._servers[server_id]
            logger.info(f"Server {server_id} removed from store")
            return True
        return False

    def get_server(self, server_id: str) -> Optional[Server]:
        """
        Get a server by ID.

        Args:
            server_id: ID of the server to retrieve

        Returns:
            Server object if found, None otherwise
        """
        return self._servers.get(server_id)

    def get_all_servers(self) -> List[Server]:
        """
        Get all servers.

        Returns:
            List of all servers
        """
        return list(self._servers.values())

    def update_location(self, server_id: str, location: Location) -> Optional[Server]:
        """
        Update the location of an existing server.

        Args:
            server_id: ID of the server
            location: New location for the server

        Returns:
            Updated server if found, None otherwise
        """
        if server_id in self._servers:
            self._servers[server_id].location = location
            logger.info(
                f"Server {server_id} location updated to ({location.x}, {location.y}, {location.z})")
            return self._servers[server_id]
        return None

    def get_server_count(self) -> int:
        """
        Get the count of servers.

        Returns:
            Number of servers
        """
        return len(self._servers)

    def clear_all(self) -> None:
        """
        Clear all servers from the store.
        """
        self._servers.clear()
        logger.info("All servers cleared from store")


# Global server store instance
_server_store: Optional[ServerStore] = None


def initialize_server_store(json_file_path: Optional[str] = None):
    """
    Initialize the global server store.

    Args:
        json_file_path: Optional path to JSON file containing server data
    """
    global _server_store
    _server_store = ServerStore(json_file_path)
    logger.info("Server store initialized")


def get_server_store() -> ServerStore:
    """
    Get the global server store instance.

    Returns:
        The global ServerStore instance

    Raises:
        RuntimeError: If store has not been initialized
    """
    if _server_store is None:
        raise RuntimeError(
            "Server store has not been initialized. Call initialize_server_store() first.")
    return _server_store
