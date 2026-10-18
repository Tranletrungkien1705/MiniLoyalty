#!/bin/bash
# Claude tu chot 1 hang muc lam tay trong worktree rieng MiniLoyalty-claude: commit -> cherry-pick vao parity/auto -> cap nhat so cai. Dung: selfcommit.sh <so> <Controller/Action>
NO=$1; ITEM=$2; R=/c/Users/Public/Pictures/kientran_private/repos; P=${PIPE_PROJECT:-MiniLoyalty}
cd $R/$P-claude || exit 1
git add -A
git -c user.name=kientran1705 -c user.email=kien.tlt.1957@aptechlearning.edu.vn commit -q -m "feat(parity): port 1-1 $ITEM tu nguon" -m "Viet tay boi Claude (agy het han muc)." -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>" || exit 2
H=$(git rev-parse HEAD)
cd $R/$P && git cherry-pick $H 2>&1 | tail -1
S=$(git rev-parse --short HEAD)
node /c/Users/Public/Pictures/kientran_private/scripts/parity/ledger-set.js $NO $S || exit 3
git add PARITY_PROGRESS.md && git -c user.name=kientran1705 -c user.email=kien.tlt.1957@aptechlearning.edu.vn commit -q -m "docs(parity): $ITEM -> Da lam" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
cd $R/$P-claude && git reset -q --hard parity/auto
