"""
Module for generating and retrieving dummy server data.
"""
import random
from datetime import datetime, timedelta
from typing import Dict, Any


def get_server_metrics(server_id: str) -> Dict[str, Any]:
    """
    Generate dummy server metrics based on server ID.
    Returns various performance and health metrics for the server.
    """
    # Use server_id as seed for consistent data per server
    random.seed(hash(server_id))

    # Generate random but realistic metrics
    cpu_usage = round(random.uniform(10.0, 95.0), 2)
    memory_usage = round(random.uniform(20.0, 90.0), 2)
    disk_usage = round(random.uniform(30.0, 85.0), 2)
    network_in = round(random.uniform(1.0, 500.0), 2)  # MB/s
    network_out = round(random.uniform(1.0, 300.0), 2)  # MB/s

    # Generate uptime (in hours)
    uptime_hours = random.randint(1, 8760)  # Up to 1 year

    # Generate last maintenance date
    days_ago = random.randint(1, 180)
    last_maintenance = (datetime.now() - timedelta(days=days_ago)).isoformat()

    # Health status based on metrics
    if cpu_usage > 90 or memory_usage > 85 or disk_usage > 80:
        health_status = "warning"
    elif cpu_usage > 95 or memory_usage > 95 or disk_usage > 90:
        health_status = "critical"
    else:
        health_status = "healthy"

    # Generate temperature data
    temperature = round(random.uniform(35.0, 85.0), 1)  # Celsius

    # Active connections
    active_connections = random.randint(50, 5000)

    # Reset random seed to avoid affecting other random operations
    random.seed()

    return {
        "server_id": server_id,
        "metrics": {
            "cpu_usage_percent": cpu_usage,
            "memory_usage_percent": memory_usage,
            "disk_usage_percent": disk_usage,
            "network_in_mbps": network_in,
            "network_out_mbps": network_out,
            "temperature_celsius": temperature,
            "active_connections": active_connections,
        },
        "status": {
            "health": health_status,
            "uptime_hours": uptime_hours,
            "last_maintenance": last_maintenance,
        },
        "metadata": {
            "timestamp": datetime.now().isoformat(),
            "data_version": "1.0",
        }
    }


def get_server_logs(server_id: str, limit: int = 5) -> Dict[str, Any]:
    """
    Generate dummy recent logs for a server.
    """
    random.seed(hash(server_id + "logs"))

    log_levels = ["INFO", "WARNING", "ERROR", "DEBUG"]
    log_messages = [
        "Service started successfully",
        "Connection established with client",
        "High memory usage detected",
        "Backup completed successfully",
        "Network latency increased",
        "Security scan completed",
        "Database connection pool exhausted",
        "Cache cleared successfully",
        "CPU throttling detected",
        "Disk I/O operations elevated"
        "Server is overloaded",
        "CPU temperature is too high",
    ]

    logs = []
    for _ in range(limit):
        minutes_ago = random.randint(1, 1440)  # Within last 24 hours
        timestamp = (datetime.now() -
                     timedelta(minutes=minutes_ago)).isoformat()

        logs.append({
            "timestamp": timestamp,
            "level": random.choice(log_levels),
            "message": random.choice(log_messages),
            "source": f"service-{random.randint(1, 5)}"
        })

    # Sort by timestamp (most recent first)
    logs.sort(key=lambda x: x["timestamp"], reverse=True)

    random.seed()

    return {
        "server_id": server_id,
        "logs": logs,
        "total_logs": limit
    }
