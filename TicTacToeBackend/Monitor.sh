#!/bin/bash

echo "=================================================="
echo "      📊 SERVER HEALTH & CAPACITY REPORT 📊       "
echo "=================================================="
echo "Date: $(date '+%Y-%m-%d %H:%M:%S')"
echo "Hostname: $(hostname)"
echo "--------------------------------------------------"

# 1. CPU CAPACITY & USAGE (The Workers)
cpu_cores=$(nproc)
cpu_load=$(top -bn1 | grep "Cpu(s)" | sed "s/.*, *\([0-9.]*\)%* id.*/\1/" | awk '{print 100 - $1}')
echo "🧠 CPU (The Brain/Workers):"
echo "   - Total Capacity: $cpu_cores Cores"
echo "   - Current Usage:  $cpu_load% utilized"
echo ""

# 2. RAM CAPACITY & USAGE (The Desk Space)
# Using 'free -m' to get values in Megabytes
total_ram=$(free -m | awk '/^Mem:/{print $2}')
used_ram=$(free -m | awk '/^Mem:/{print $3}')
ram_percent=$(awk "BEGIN {printf \"%.2f\", ($used_ram/$total_ram)*100}")
echo "🗄️  RAM (The Desk Space):"
echo "   - Total Capacity: ${total_ram} MB"
echo "   - Currently Used: ${used_ram} MB (${ram_percent}% full)"
echo ""

# 3. DISK CAPACITY (The Filing Cabinet)
# Checking the root partition '/'
total_disk=$(df -h / | awk 'NR==2 {print $2}')
used_disk=$(df -h / | awk 'NR==2 {print $3}')
disk_percent=$(df -h / | awk 'NR==2 {print $5}')
echo "📁 STORAGE (The Filing Cabinet):"
echo "   - Total Capacity: $total_disk"
echo "   - Currently Used: $used_disk ($disk_percent full)"
echo ""

# 4. ACTIVE APPLICATIONS (The Departments)
echo "🏢 RUNNING DEPARTMENTS (Docker Containers):"
if command -v docker &> /dev/null; then
    docker ps --format "   - 🟢 {{.Names}} (Running for {{.RunningFor}})"
else
    echo "   - Docker is not installed or not running."
fi
echo ""

# 5. HEAVIEST TASKS (Who is using the most resources?)
echo "⚠️  TOP 3 RESOURCE HEAVY TASKS:"
ps -eo comm,pcpu,pmem --sort=-pcpu | head -n 4 | awk 'NR>1 {printf "   - %-15s | CPU: %-5s%% | RAM: %-5s%%\n", $1, $2, $3}'

echo "=================================================="