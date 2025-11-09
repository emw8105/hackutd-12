from typing import Dict, List, Optional
from models import Technician, Location
import logging
import threading

logger = logging.getLogger(__name__)


class TechnicianStore:

    def __init__(self):
        self._technicians: Dict[str, Technician] = {}
        self.lock = threading.Lock()

    def add_technician(self, technician: Technician) -> Technician:
        """
        Add or update a technician in the store.

        Args:
            technician: Technician object with id and location

        Returns:
            The added/updated technician
        """
        with self.lock:
            self._technicians[technician.id] = technician
            logger.info(
                f"Technician {technician.id} added/updated at location ({technician.location.x}, {technician.location.y}, {technician.location.z})"
            )
            return technician

    def remove_technician(self, technician_id: str) -> bool:
        """
        Remove a technician from the store.

        Args:
            technician_id: ID of the technician to remove

        Returns:
            True if technician was removed, False if not found
        """
        with self.lock:
            if technician_id in self._technicians:
                del self._technicians[technician_id]
                logger.info(f"Technician {technician_id} removed from store")
                return True
            return False

    def get_technician(self, technician_id: str) -> Optional[Technician]:
        """
        Get a technician by ID.

        Args:
            technician_id: ID of the technician to retrieve

        Returns:
            Technician object if found, None otherwise
        """
        with self.lock:
            return self._technicians.get(technician_id)

    def get_all_technicians(self) -> List[Technician]:
        """
        Get all active technicians.

        Returns:
            List of all active technicians
        """
        with self.lock:
            return list(self._technicians.values())

    def update_location(
        self, technician_id: str, location: Location
    ) -> Optional[Technician]:
        """
        Update the location of an existing technician.

        Args:
            technician_id: ID of the technician
            location: New location for the technician

        Returns:
            Updated technician if found, None otherwise
        """
        with self.lock:
            if technician_id in self._technicians:
                self._technicians[technician_id].location = location
                logger.info(
                    f"Technician {technician_id} location updated to ({location.x}, {location.y}, {location.z})"
                )
                return self._technicians[technician_id]
            return None

    def get_technician_count(self) -> int:
        """
        Get the count of active technicians.

        Returns:
            Number of active technicians
        """
        with self.lock:
            return len(self._technicians)

    def clear_all(self) -> None:
        """
        Clear all technicians from the store.
        """
        with self.lock:
            self._technicians.clear()
            logger.info("All technicians cleared from store")


# Global technician store instance
_technician_store: Optional[TechnicianStore] = None


def initialize_technician_store():
    """Initialize the global technician store."""
    global _technician_store
    _technician_store = TechnicianStore()
    logger.info("Technician store initialized")


def get_technician_store() -> TechnicianStore:
    """
    Get the global technician store instance.

    Returns:
        The global TechnicianStore instance

    Raises:
        RuntimeError: If store has not been initialized
    """
    if _technician_store is None:
        raise RuntimeError(
            "Technician store has not been initialized. Call initialize_technician_store() first."
        )
    return _technician_store
