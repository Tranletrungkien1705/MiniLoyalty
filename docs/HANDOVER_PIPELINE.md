# HANDOVER — Pipeline port 1-1 bằng AI agent phụ (cập nhật 2026-10-18)

Mục tiêu: port mã ERP cũ (`D:\idocNet\…`) sang .NET 8 repo `Mini*` (nhánh `parity/auto`), chạy 24/7 bằng các agent phụ, **không cần Claude**, tới **2026-10-20 23:00**.

## 1. Tiến độ (lúc 2026-10-18 22:20)
| Dự án | Nguồn | Tiến độ |
|---|---|---|
| MiniLoyalty | 2022.7.Loyalty | 320/337 (xong; còn 17 mục Bỏ/cần xem tay) |
| MiniGate | 2019.9.MobileGate | 9 làm, 740 bỏ (forwarder). Vòng 2: `MiniGate\.parity-run\reopen_candidates.txt` (52 mục) |
| MiniContract | 2020.6.QContract | ~76/295 (đang chạy) |
| MiniWMS | 2020.3.Skycic.Inventory | 0/521 (đã dựng hạ tầng, ledger, rules) |
| MiniService | 2023.H.CarServices | 0/1152 (2 tầng, best-effort) |
| MiniDMS | 2023.3.DMS.Sales | CHƯA: repo `src/MiniDMS.Web` khác bố cục; cần profile thủ công |

## 2. Kiến trúc
- `scripts\parity\` : `parity-step.ps1` (prep|run|fix|commit|drop|status), `lead.ps1`, `feeder.ps1`, `extract-source.ps1`, `gen-ledger.ps1`, `profile.ps1` (hồ sơ dự án: thêm 1 nhánh switch), `doc-gw/doc-all` (tài liệu bằng Gemini web).
- `scripts\pipe\` : `supervisor.ps1` (**bộ não không LLM**), `start-all-slots.ps1`, `auto-loop.ps1` (50 s/vòng: auto-bo, chrome-clean, slot-watch, auto-commit, status), `bootstrap-port-infra.ps1`.
- Chọn dự án bằng env `PIPE_PROJECT`. Runtime ở `repos\<Project>\.parity-run\` (run.log, status.md, slots.json, skipped.json, logs\).
- Auto-commit chỉ chốt khi: chỉ đổi Controllers/Port, Models/Port, *.Tests; mã lỗi hợp lệ; không có mô phỏng/mock; build + test xanh (chạy lại 2 lần). Lỗi → dọn, trả hạng mục về kho.
- Auto-bo: forwarder thuần sang dịch vụ ngoài → Bỏ; kẹt >20 phút → release.

## 3. Cấu hình agent phụ tối ưu (đã đo)
- **Router** (`PARITY_ENGINE_FORCE=rtr`): `claude.exe` trỏ `ANTHROPIC_BASE_URL=http://127.0.0.1:20128` (9router, combo parity-gemini/parity-claude, 4 tài khoản antigravity). Token ≈ agy trực tiếp. `--max-turns 160`.
- agy trực tiếp hết quota tuần tới 2026-10-24 → mọi slot agy/agy2–agy6 chạy qua router.
- codegpt: hay lỗi 21–37 phút → TẮT. gweb: 1 Chrome (CDP 9222, `scripts\pipe\chrome-ports.txt`); đầu ra gweb dễ bịa thực thể khi excerpt thiếu, auto-commit sẽ chặn.
- RAM 16 GB: giữ ≤1–2 Chrome. Máy phải cắm điện (supervisor đặt standby=0).
- Mỗi mục ~0.15–0.32M token mới + 1–2.5M cache-read.

## 4. Runbook
- Khởi động: `Start-Process -WindowStyle Hidden powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File scripts\pipe\supervisor.ps1 -Push'` (hàng đợi MiniContract > MiniWMS > MiniService > MiniGate; hạn 2026-10-20 23:00).
- Xem nhanh: `repos\supervisor.log`, `repos\<P>\.parity-run\status.md`, `docs\FINAL_REPORT.md` (khi xong/hết hạn).
- Dừng: tạo `repos\SUPERVISOR_STOP`, và `repos\<P>\.parity-run\STOP`.
- Sau khi reboot: chạy lại lệnh khởi động supervisor + 9router + Chrome 9222 (đã đăng nhập Gemini).
- Dự án mới: thêm nhánh trong `profile.ps1`, tạo `docs\PORT_RULES_<P>.md` (sao từ MiniContract), `bootstrap-port-infra.ps1 -Project X -ErrPrefix …`, `gen-ledger.ps1`, copy `FreePort.cs` vào Tests.

## 5. Bài học
Xem `docs\LESSONS_AGENTS.md`. Tóm tắt: regex QUOTA không bắt "429" trần; Chrome hỏng khi khôi phục nhiều tab; test dùng `FreePort.Get()`; mỗi worker một controller riêng (xung đột file); excerpt thiếu → agent bịa → phải từ chối; nhận diện forwarder cần cả `RestClient`, `WAS_/WA_`, helper trong file gate.

## 6. Vấn đề đã biết / việc tay
- Tailscale chạy dưới user khác → không điều khiển được; user tự chạy forwarder nếu cần mở 9router qua LAN (9router chỉ bind 127.0.0.1).
- Quota router là phụ thuộc chính; hết quota → worker tự nghỉ (cooldown) rồi chạy lại.
- MiniGate vòng 2, MiniDMS, 17 mục Loyalty cần xem tay. Không đụng `D:\idocNet\_labs\MiniHTC`, không push master.
- Không commit transcript phiên Claude (chứa tiền tố token OAuth).

## 7. Cập nhật 2026-10-18 22:50
- Tự nạp khi bật máy: `Startup\pipeline-boot.cmd` → `scripts\pipe\boot.ps1` (9router, Chrome 9222, supervisor). Cầu CodeGPT (cổng 54112) là tiến trình của VS Code; supervisor tự mở VS Code nếu cầu chết.
- Chrome gweb chạy ẩn (`scripts\pipe\chrome-headless.txt`; xoá file để hiện cửa sổ). RAM ~1 GB/profile, ẩn không nhẹ hơn nhiều.
- codegpt: nguyên nhân lỗi là *một hạng mục* (#127) khiến run treo >600 s phía server (81 msg, không kết thúc), không phải bridge hỏng. Supervisor ghi hạng mục lỗi ≥2 lần vào `codegpt_skip.txt` rồi codegpt làm hạng mục khác. Feeder là vòng lặp tự chạy lại.
- gweb: `gweb_all.txt` trong `.parity-run` cho gweb nhận cả hạng mục ghi (trước chỉ đọc); auto-commit vẫn chặn kết quả sai.
- Settings: thêm `Bash(git:*)`, `PowerShell(git:*)`.
