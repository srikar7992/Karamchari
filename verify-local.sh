#!/bin/bash
# verify-local.sh

echo "===================================================="
echo " Karamchari Day-1 Stack Smoke Test Verification"
echo "===================================================="

check_port() {
  local service_name=$1
  local host=$2
  local port=$3
  
  if nc -z "$host" "$port" >/dev/null 2>&1; then
    printf "%-25s %-10s %-10s\n" "$service_name" "$port" "PASS"
    return 0
  else
    printf "%-25s %-10s %-10s\n" "$service_name" "$port" "FAIL"
    return 1
  fi
}

printf "%-25s %-10s %-10s\n" "Service" "Port" "Status"
printf "%-25s %-10s %-10s\n" "-------" "----" "------"

check_port "SQL Server" "localhost" 1433
check_port "RabbitMQ" "localhost" 5672
check_port "Redis" "localhost" 6379
check_port "API Port" "localhost" 60462

# Verify Health check endpoint
if nc -z localhost 60462 >/dev/null 2>&1; then
  # API is up, let's curl health
  health_response=$(curl -k -s -m 3 https://localhost:60462/health)
  if [[ "$health_response" == *"Healthy"* ]]; then
    printf "%-25s %-10s %-10s\n" "API Health Endpoint" "60462" "PASS"
  else
    printf "%-25s %-10s %-10s\n" "API Health Endpoint" "60462" "FAIL (Unhealthy)"
  fi
else
  printf "%-25s %-10s %-10s\n" "API Health Endpoint" "60462" "FAIL (API Offline)"
fi

echo "===================================================="
