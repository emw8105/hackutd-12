from __future__ import annotations

from ortools.graph.python import min_cost_flow
import numpy as np
import threading
import copy
import logging

import config
import models

# # Sample Data
# technicians = ["T1", "T2", "T3"]
# tasks = ["TaskA", "TaskB", "TaskC", "TaskD"]

# # Distances (e.g., travel time in minutes)
# # Rows: Technicians, Columns: Tasks
# distances = np.array(
#     [
#         [10, 5, 12, 8],  # T1's distance to A, B, C, D
#         [7, 15, 6, 11],  # T2's distance to A, B, C, D
#         [9, 8, 10, 14],  # T3's distance to A, B, C, D
#     ]
# )

# # Task Priority (higher is better, e.g., 1-10)
# task_priorities = np.array([5, 8, 2, 7])
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class TaskAssigner:
    def __init__(
        self,
        technicians: list[str] = [],
        tasks: list[models.JiraTicket] = [],
        distances: list[list[int]] = [],
        priority_weight: float = config.TASK_PRIORITY_WEIGHT,
    ):
        self.priority_weight = priority_weight
        self.graph: "Graph" | None = None
        self.technicians: list[str] = technicians
        self.distances: list[list[int]] = distances
        self.tasks: list[models.JiraTicket] = tasks
        self.assignments = {}
        self.lock = threading.Lock()
        logger.info("Initialized TaskAssigner")

    def update_floor(
        self,
        technicians: list[str],
        distances: list[list[int]],
    ) -> None:
        with self.lock:
            self.technicians = technicians
            self.distances = distances
            self._rebuild_graph_unsafe()

    def add_technician(self, technician: str, distances: list[int]) -> None:
        with self.lock:
            self.technicians.append(technician)
            self.distances.append(distances)
            self._rebuild_graph_unsafe()

    def _CONSTANT_PRIORITIES(self, num_tasks: int) -> np.ndarray:
        return np.array([1] * num_tasks)

    def _rebuild_graph_unsafe(self):
        if self.technicians and self.tasks and self.distances:
            self.graph = Graph(
                technicians=self.technicians,
                tasks=self.tasks,
                distances=np.array(self.distances),
                task_priorities=self._CONSTANT_PRIORITIES(len(self.tasks)),
                priority_weight=config.TASK_PRIORITY_WEIGHT,
            )

    def set_distances(self, distances: list[list[int]]) -> None:
        with self.lock:
            self.distances = distances
            self._rebuild_graph_unsafe()

    def refresh_technicians(
        self,
        technicians: list[str],
    ) -> None:
        with self.lock:
            self.technicians = technicians
            self._rebuild_graph_unsafe()

    def refresh_tasks(
        self,
        tasks: list[models.JiraTicket],
    ) -> None:
        logging.info("Refreshing tasks in TaskAssigner")
        with self.lock:
            self.tasks = copy.deepcopy(tasks)
            self._rebuild_graph_unsafe()

    def assign_tasks(self) -> dict[str, models.JiraTicket]:
        with self.lock:
            if self.graph:
                self.assignments = self.graph.solve_graph()
        return self.assignments


class Graph:
    def __init__(
        self,
        technicians: list[str],
        tasks: list[str],
        distances: np.ndarray,
        task_priorities: np.ndarray,
        priority_weight: float,
    ):
        self.technicians = copy.deepcopy(technicians)
        self.tasks = copy.deepcopy(tasks)
        self.distances = copy.deepcopy(distances)
        self.task_priorities = copy.deepcopy(task_priorities)
        self.priority_weight = priority_weight
        self.smcf, self.cost_offset = Graph._create_graph(
            technitions=self.technicians,
            tasks=self.tasks,
            distances=self.distances,
            task_priorities=self.task_priorities,
            priority_weight=self.priority_weight,
        )

    def solve_graph(self):
        status = self.smcf.solve()
        if status != self.smcf.OPTIMAL:
            raise ValueError("The solver did not find an optimal solution.")

        assignments = {}

        num_techs = len(self.technicians)
        num_tasks = len(self.tasks)

        # Tech-to-task arcs start at index num_techs
        start_index = num_techs
        end_index = num_techs + num_techs * num_tasks

        # Loop through original combined list of arcs.
        for arc_index in range(start_index, end_index):
            flow = self.smcf.flow(arc_index)

            # An assignment has occurred if flow is 1
            if flow > 0:
                tech_idx = self.smcf.tail(arc_index) - 1
                task_idx = self.smcf.head(arc_index) - (num_techs + 1)
                cost = self.smcf.unit_cost(arc_index) - self.cost_offset

                assignments[self.technicians[tech_idx]] = self.tasks[task_idx]

        return assignments

    def _create_graph(
        technitions: list[str],
        tasks: list[str],
        distances: np.ndarray,
        task_priorities: np.ndarray,
        priority_weight: float,
    ) -> tuple[min_cost_flow.SimpleMinCostFlow, float]:
        """
        Create and return a min cost flow graph for assigning technicians to tasks
        """

        costs = (distances - priority_weight * task_priorities).astype(int)
        # Convert all costs to int for OR-Tools
        costs = costs.astype(int)
        # OR-Tools requires costs to be non-negative.
        min_cost = np.min(costs)
        cost_offset = 0 if min_cost >= 0 else abs(min_cost)
        non_negative_costs = costs + cost_offset

        num_techs = len(technitions)
        num_tasks = len(tasks)

        SOURCE_NODE = 0
        SINK_NODE = num_techs + num_tasks + 1
        TECH_NODES = range(1, num_techs + 1)
        TASK_NODES = range(num_techs + 1, num_techs + num_tasks + 1)

        smcf = min_cost_flow.SimpleMinCostFlow()

        # Source -> Technician arcs
        source_to_tech_starts = [SOURCE_NODE] * num_techs
        source_to_tech_ends = list(TECH_NODES)
        source_to_tech_capacities = [1] * num_techs
        source_to_tech_costs = [0] * num_techs

        # Technician -> Task arcs
        tech_to_task_starts = []
        tech_to_task_ends = []
        for i in range(num_techs):
            for j in range(num_tasks):
                tech_to_task_starts.append(TECH_NODES[i])
                tech_to_task_ends.append(TASK_NODES[j])
        tech_to_task_costs = non_negative_costs.flatten().tolist()
        tech_to_task_capacities = [1] * len(tech_to_task_costs)

        # Task -> Sink arcs
        task_to_sink_starts = list(TASK_NODES)
        task_to_sink_ends = [SINK_NODE] * num_tasks
        task_to_sink_capacities = [1] * num_tasks
        task_to_sink_costs = [0] * num_tasks

        # Combine all arcs
        start_nodes = source_to_tech_starts + tech_to_task_starts + task_to_sink_starts
        end_nodes = source_to_tech_ends + tech_to_task_ends + task_to_sink_ends
        capacities = (
            source_to_tech_capacities
            + tech_to_task_capacities
            + task_to_sink_capacities
        )
        costs_list = source_to_tech_costs + tech_to_task_costs + task_to_sink_costs

        # Add the arcs
        for i in range(len(start_nodes)):
            smcf.add_arc_with_capacity_and_unit_cost(
                start_nodes[i], end_nodes[i], capacities[i], costs_list[i]
            )

        # Define the supply/demand for the nodes (source and sink)
        supplies = [0] * (SINK_NODE + 1)  # All nodes start with 0 supply
        supplies[SOURCE_NODE] = num_techs
        supplies[SINK_NODE] = -min(
            num_tasks, num_techs
        )  # Sink demands flow maxed by technicians.

        for i in range(len(supplies)):
            smcf.set_node_supply(i, supplies[i])

        return (smcf, cost_offset)


task_assigner: TaskAssigner = TaskAssigner(priority_weight=config.TASK_PRIORITY_WEIGHT)
