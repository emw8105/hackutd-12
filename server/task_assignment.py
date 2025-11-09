from ortools.graph.python import min_cost_flow
import numpy as np
from dataclasses import dataclass
import copy


@dataclass
class Graph:
    smcf: min_cost_flow.SimpleMinCostFlow
    cost_offset: int
    technicians: list[str]
    tasks: list[str]


def create_graph(
    technitions: list[str],
    tasks: list[str],
    distances: np.ndarray,
    task_priorities: np.ndarray,
    priority_weight: float,
) -> Graph:
    """
    Create and return a min cost flow graph for assigning technicians to tasks
    """

    costs = distances - priority_weight * task_priorities
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
        source_to_tech_capacities + tech_to_task_capacities + task_to_sink_capacities
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

    return Graph(
        smcf=smcf,
        cost_offset=cost_offset,
        technicians=copy.deepcopy(technitions),
        tasks=copy.deepcopy(tasks),
    )


def solve_graph(graph: Graph):
    status = graph.smcf.solve()
    if status != graph.smcf.OPTIMAL:
        raise ValueError("The solver did not find an optimal solution.")

    num_techs = len(graph.technicians)
    num_tasks = len(graph.tasks)

    # Tech-to-task arcs start at index num_techs
    start_index = num_techs
    end_index = num_techs + num_techs * num_tasks

    # Loop through original combined list of arcs.
    for arc_index in range(start_index, end_index):
        flow = smcf.flow(arc_index)

        # An assignment has occurred if flow is 1
        if flow > 0:
            tech_idx = smcf.tail(arc_index) - 1
            task_idx = smcf.head(arc_index) - (num_techs + 1)
            cost = smcf.unit_cost(arc_index) - cost_offset
            print(
                f"Technician {technicians[tech_idx]} assigned to Task {tasks[task_idx]} with cost {cost}"
            )


# Sample Data
technicians = ["T1", "T2", "T3"]
tasks = ["TaskA", "TaskB", "TaskC", "TaskD"]

# Distances (e.g., travel time in minutes)
# Rows: Technicians, Columns: Tasks
distances = np.array(
    [
        [10, 5, 12, 8],  # T1's distance to A, B, C, D
        [7, 15, 6, 11],  # T2's distance to A, B, C, D
        [9, 8, 10, 14],  # T3's distance to A, B, C, D
    ]
)

# Task Priority (higher is better, e.g., 1-10)
task_priorities = np.array([5, 8, 2, 7])

# Weighting factor for priority (tune this to adjust priority's impact)
WEIGHT_P = 2

graph = create_graph(
    technitions=technicians,
    tasks=tasks,
    distances=distances,
    task_priorities=task_priorities,
    priority_weight=WEIGHT_P,
)

smcf = graph.smcf
cost_offset = graph.cost_offset
num_techs = len(technicians)
num_tasks = len(tasks)

solve_graph(graph)
