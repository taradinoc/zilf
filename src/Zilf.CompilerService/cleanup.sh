#!/bin/bash
# Cleanup old build files
# Delete files older than 1 day from Projects and Stash directories

WORK_ROOT="${ZILF_WORK_ROOT:-/work}"
PROJECTS_DIR="$WORK_ROOT/Projects"
STASH_DIR="$WORK_ROOT/Stash"

# Projects directory
if [ -d "$PROJECTS_DIR" ]; then
    echo "$(date): Cleaning up $PROJECTS_DIR"
    find "$PROJECTS_DIR" -mindepth 1 -maxdepth 1 -type d -mtime +1 -exec rm -rf {} \;
fi

# Stash directory
if [ -d "$STASH_DIR" ]; then
    echo "$(date): Cleaning up $STASH_DIR"
    find "$STASH_DIR" -mindepth 1 -maxdepth 1 -type d -mtime +1 -exec rm -rf {} \;
fi

echo "$(date): Cleanup complete"
