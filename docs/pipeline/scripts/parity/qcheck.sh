#!/bin/bash
# Kiem nhanh 1 slot: build, test, chay app tam, goi cac body, tat app. Dung: [PIPE_PROJECT=MiniGate] qcheck.sh <worker> <port> <path> <body1> [body2...]
# Chi reset len parity/auto khi worktree CHUA co thay doi (tranh xung dot stash-pop cua ban cu); co thay doi thi giu nguyen cay lam viec.
W=$1; P=$2; U=$3; shift 3
PJ=${PIPE_PROJECT:-MiniLoyalty}
cd /c/Users/Public/Pictures/kientran_private/repos/$PJ-$W || exit 1
if [ -z "$(git status --porcelain)" ]; then git reset -q --hard parity/auto; fi
dotnet build -c Release -clp:ErrorsOnly --nologo 2>&1 | grep -E "error|Build succ" | sort -u | head -3
dotnet test $PJ.Tests -c Release --nologo 2>&1 | grep -E "Passed!|Failed!|Failed "
export PORT=$P CONNECTION_STRING="Data Source=C:/Users/kien2/AppData/Local/Temp/q_$P.db"; rm -f /c/Users/kien2/AppData/Local/Temp/q_$P.db*
(dotnet run -c Release --no-build --no-launch-profile > /dev/null 2>&1 &)
for i in $(seq 1 40); do curl -s -o /dev/null http://127.0.0.1:$P/ && break; sleep 2; done
for b in "$@"; do echo "== $b"; curl -s -X POST http://127.0.0.1:$P$U -H 'Content-Type: application/json' -d "$b" | head -c 420; echo; done
powershell -NoProfile -Command "Get-CimInstance Win32_Process | ? { \$_.CommandLine -match '$PJ-$W\b' -and \$_.Name -match 'dotnet|$PJ' } | % { Stop-Process -Id \$_.ProcessId -Force }"
