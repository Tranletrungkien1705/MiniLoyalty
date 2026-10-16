# BÁO CÁO TIẾN ĐỘ PORT 1-1 (PROGRESS REPORT) — MiniLoyalty

> **Phạm vi**: Toàn bộ 337 endpoint / action thuộc 67 controller trong giải pháp `MiniLoyalty`.  
> **Cập nhật**: 2026-10-16 12:12  
> **Nguồn dữ liệu**: Đo lường thực tế từ `PARITY_PROGRESS.md`, `.parity-run\skipped.json`, và `git log`.  
> **Trạng thái**: Nháp — chờ review  

---

## 1. Tổng quan tiến độ

- **Tiến độ tổng thể**: **123 / 337 (36.50%)**
- **Đã làm (Hoàn thành)**: **123 mục (36.50%)** — toàn bộ đã được Claude review (thân commit ghi `Review: ... PASS`) và có commit hợp lệ trên nhánh `parity/auto`.
- **Bỏ (Loại bỏ)**: **1 mục (0.30%)** — mục scaffold/backup nội bộ không mang giá trị vận hành (`Mst_OrgController/GetAllOrg_bk`, #180).
- **Chưa làm (Tồn đọng)**: **213 mục (63.20%)** — gồm 5 mục tạm hoãn / cần xem tay và 208 mục chờ chuyển đổi theo kế hoạch.
- **Tạm hoãn / Cần xem tay**: **5 mục** (theo `.parity-run\skipped.json`: 3 mục `CAN_XEM_TAY`, 2 mục `HOAN_PHU_THUOC`).

### Bảng chỉ số tổng quan

| Chỉ số đo lường | Giá trị số | Tỷ lệ (%) | Ghi chú phương pháp đo |
|---|---|---|---|
| Tổng mẫu số (N) | **337** | 100.00% | Đếm số dòng bảng định dạng `\| <số> \|` trong `PARITY_PROGRESS.md` |
| Đã hoàn thành (X) | **123** | 36.50% | Lọc trạng thái `Đã làm` và xác minh mã commit tồn tại trong `git log` |
| Đã loại bỏ | **1** | 0.30% | Lọc trạng thái `Bỏ` tại hàng #180 (`GetAllOrg_bk`) |
| Tồn đọng cần làm | **213** | 63.20% | Lọc trạng thái `Chưa làm` trong sổ tiến độ (213 + 123 + 1 = 337) |
| Tổng số Controller | **67** | — | 27 controller có tiến độ, 21 controller đạt 100%, 40 controller chưa làm |
| Tạm hoãn kỹ thuật | **5** | 1.48% | Trích xuất từ `.parity-run\skipped.json` (thuộc tập Chưa làm) |

---

## 2. Thống kê theo Nhóm nghiệp vụ (8 nhóm tiền tố)

Toàn bộ 337 hạng mục được phân thành 8 nhóm nghiệp vụ theo tiền tố định danh của controller:

| STT | Nhóm tiền tố | Ý nghĩa nghiệp vụ | Tổng số mục | Đã làm | Bỏ | Chưa làm | Tỷ lệ hoàn thành (%) |
|---|---|---|---|---|---|---|---|
| 1 | `Mst_` | Danh mục cơ sở (Master Data) | 178 | 119 | 1 | 58 | 66.85% |
| 2 | `Map_` | Bảng liên kết trung gian (Mapping) | 5 | 4 | 0 | 1 | 80.00% |
| 3 | `Crd_` | Nghiệp vụ thẻ & Hội viên (Card / Member) | 58 | 0 | 0 | 58 | 0.00% |
| 4 | `Sys_` | Quản trị hệ thống & Phân quyền (System) | 38 | 0 | 0 | 38 | 0.00% |
| 5 | `Prm_` | Chương trình khuyến mại (Promotion) | 25 | 0 | 0 | 25 | 0.00% |
| 6 | `Rpt_` | Báo cáo thống kê (Report) | 16 | 0 | 0 | 16 | 0.00% |
| 7 | `Khác` | Tiện ích, Home, Upload file | 15 | 0 | 0 | 15 | 0.00% |
| 8 | `InvF_` | Mẫu in & Hóa đơn (Invoice / Template) | 2 | 0 | 0 | 2 | 0.00% |
| **Tổng** | **Toàn bộ hệ thống** | — | **337** | **123** | **1** | **213** | **36.50%** |

### Đánh giá tiến độ theo nhóm
1. **Nhóm Danh mục cơ sở (`Mst_*`)**: Đạt **66.85%** (119/178 mục, tăng 10 mục so với lượt 1). Đã hoàn thành dứt điểm 18 controller danh mục (Tỉnh thành, Quận huyện, Phường xã, Thương hiệu xe, Dòng xe, Loại thẻ, Kỷ luật, Phòng ban, Tình trạng gia đình, Ngạch bậc, v.v.) và đang triển khai các danh mục tiếp theo (`Mst_GovIDType`, `Mst_Gender`, `Mst_LeaveDay`). Khối lượng này tạo nền tảng vững chắc để chuyển sang các bảng giao dịch nghiệp vụ.
2. **Nhóm Liên kết đại lý (`Map_*`)**: Đạt **80.00%** (4/5 mục). Chỉ còn duy nhất 1 mục `#068 Map_QueryDealer_MemberController/Create` đang tạm hoãn do phụ thuộc Web Service Hyundai DMS.
3. **Các nhóm nghiệp vụ giao dịch**: Các nhóm `Crd_*`, `Prm_*`, `Rpt_*`, `Sys_*`, `InvF_*`, `Khác` hiện đang ở mức 0% và sẽ được triển khai tuần tự ngay sau khi dứt điểm các danh mục `Mst_*` còn lại.

---

## 3. Danh sách các hạng mục bị bỏ qua & Tạm hoãn

### 3.1. Hạng mục loại bỏ chính thức trong sổ tiến độ (`Bỏ`)

| # | Hạng mục | Trạng thái | Lý do loại bỏ chi tiết |
|---|---|---|---|
| 180 | `Mst_OrgController/GetAllOrg_bk` | Bỏ | Phương thức sao lưu nội bộ (backup) của lập trình viên cũ, không được cổng API ClientGate tham chiếu trong nghiệp vụ thực tế. |

### 3.2. Hạng mục tạm hoãn kỹ thuật (`skipped.json`)

Năm hạng mục sau được ghi nhận tạm hoãn trong quá trình điều phối do các rào cản phụ thuộc hạ tầng ngoại vi:

| # | Hạng mục | Nhãn phân loại | Lý do kỹ thuật chi tiết | Hướng xử lý đề xuất |
|---|---|---|---|---|
| 181 | `Mst_OrgController/GetAllOrg` | `CAN_XEM_TAY` | Phương thức gọi API dịch vụ ngoài iNOS Master Server (`WA_OS_Inos_Org_GetMyOrgList` tại `BaseMasterServerAPIAddress`) để lấy danh sách tổ chức chưa có trong `Mst_Org`. MiniLoyalty chưa có hạ tầng đăng ký Master Server. | Chờ người dùng quyết định: (a) Tạo bảng mirror `OS_Inos_Org`; (b) Gọi API thật; hoặc (c) Loại bỏ nếu không tích hợp iNOS. |
| 182 | `Mst_OrgController/GetOrgsNotInSolution` | `CAN_XEM_TAY` | Tương tự #181, phụ thuộc vào API dịch vụ ngoài iNOS Master Server `WA_OS_Inos_Org_GetMyOrgList`. | Chờ người dùng quyết định phương án tích hợp tương tự #181. |
| 115 | `Mst_DealerCPController/Create` | `HOAN_PHU_THUOC` | Gọi `Mst_NNT_CreateForNetworkCP` (ghi khoảng 45 cột của bảng `Mst_NNT`, kiểm tra `Sys_User_GetAbilityViewOfUser`) và `Map_DealerCP_Org_Save`. Bản CreateOld (#114) đã port thành công. | Hoãn lại, thực hiện sau khi hoàn thành các hạng mục `Mst_NNTController/*`, `Map_DealerCP_OrgController/*` và module `Sys_User`. |
| 068 | `Map_QueryDealer_MemberController/Create` | `CAN_XEM_TAY` | Phần lưu database nội bộ port được, nhưng sau khi lưu có gọi Web Service ngoài của Hyundai DMS (`WSDMS CarSv_Ser_CustomerCar_Get`, `SerCarUpdate_KeyPlateNo`, `SerCarUpdate_KeyVIN`, `Ser_CustomerCar_SalesCreate`) ghi dữ liệu sang hệ thống khác; lỗi WS làm fail cả transaction. | Chờ người dùng quyết định: stub Web Service DMS, port phần DB kèm ghi nhận GAP, hoặc bỏ endpoint. |
| 119 | `Mst_DealerCPController/Import` | `HOAN_PHU_THUOC` | Tương tự #115, phương thức gọi `WA_Mst_NNT_CreateForNetworkCP` ghi toàn bộ bảng `Mst_NNT` phức tạp và kiểm tra quyền người dùng `Sys_User`. | Hoãn lại, thực hiện sau khi hoàn thành `Mst_NNTController/*` và `Sys_User`. |

---

## 4. Bảng thống kê chi tiết toàn bộ 67 Controller

| STT | Tên Controller | Tổng số action | Đã làm | Bỏ | Chưa làm | Tỷ lệ hoàn thành (%) |
|---|---|---|---|---|---|---|
| 1 | `Crd_CardController` | 6 | 0 | 0 | 6 | 0.0% |
| 2 | `Crd_CardRankController` | 5 | 0 | 0 | 5 | 0.0% |
| 3 | `Crd_CardTransactionController` | 3 | 0 | 0 | 3 | 0.0% |
| 4 | `Crd_CardTransactionInCreaseController` | 2 | 0 | 0 | 2 | 0.0% |
| 5 | `Crd_CardTransactionServiceTurnController` | 2 | 0 | 0 | 2 | 0.0% |
| 6 | `Crd_DealSerROController` | 3 | 0 | 0 | 3 | 0.0% |
| 7 | `Crd_DealUsePromotionController` | 5 | 0 | 0 | 5 | 0.0% |
| 8 | `Crd_MemberChangeInfoController` | 12 | 0 | 0 | 12 | 0.0% |
| 9 | `Crd_MemberController` | 16 | 0 | 0 | 16 | 0.0% |
| 10 | `Crd_MemberDiscountTransactionController` | 1 | 0 | 0 | 1 | 0.0% |
| 11 | `Crd_MemberVoucherTransactionController` | 3 | 0 | 0 | 3 | 0.0% |
| 12 | `FileController` | 1 | 0 | 0 | 1 | 0.0% |
| 13 | `HomeController` | 3 | 0 | 0 | 3 | 0.0% |
| 14 | `InvF_TempPrintController` | 2 | 0 | 0 | 2 | 0.0% |
| 15 | `Map_DealerCP_OrgController` | 1 | 1 | 0 | 0 | 100.0% |
| 16 | `Map_QueryDealer_MemberController` | 4 | 3 | 0 | 1 | 75.0% |
| 17 | `Mst_BirthPolicyController` | 7 | 7 | 0 | 0 | 100.0% |
| 18 | `Mst_CarBrandController` | 10 | 10 | 0 | 0 | 100.0% |
| 19 | `Mst_CardTypeController` | 10 | 10 | 0 | 0 | 100.0% |
| 20 | `Mst_CarModelController` | 10 | 10 | 0 | 0 | 100.0% |
| 21 | `Mst_CertificateGroupController` | 2 | 2 | 0 | 0 | 100.0% |
| 22 | `Mst_CountryController` | 1 | 1 | 0 | 0 | 100.0% |
| 23 | `Mst_DealerCPController` | 12 | 10 | 0 | 2 | 83.3% |
| 24 | `Mst_DepartmentController` | 10 | 10 | 0 | 0 | 100.0% |
| 25 | `Mst_DisciplineController` | 7 | 7 | 0 | 0 | 100.0% |
| 26 | `Mst_DistrictController` | 10 | 10 | 0 | 0 | 100.0% |
| 27 | `Mst_EthnicCategoryController` | 2 | 2 | 0 | 0 | 100.0% |
| 28 | `Mst_ExpenseTypeController` | 1 | 1 | 0 | 0 | 100.0% |
| 29 | `Mst_FamilyStatusController` | 2 | 2 | 0 | 0 | 100.0% |
| 30 | `Mst_GenderController` | 2 | 1 | 0 | 1 | 50.0% |
| 31 | `Mst_GovIDTypeController` | 6 | 3 | 0 | 3 | 50.0% |
| 32 | `Mst_GradeController` | 2 | 2 | 0 | 0 | 100.0% |
| 33 | `Mst_LeaveDayController` | 2 | 1 | 0 | 1 | 50.0% |
| 34 | `Mst_LeaveTypeController` | 2 | 0 | 0 | 2 | 0.0% |
| 35 | `Mst_MajorController` | 2 | 0 | 0 | 2 | 0.0% |
| 36 | `Mst_MajorGroupController` | 2 | 0 | 0 | 2 | 0.0% |
| 37 | `Mst_NNTController` | 6 | 0 | 0 | 6 | 0.0% |
| 38 | `Mst_OrgController` | 5 | 2 | 1 | 2 | 40.0% |
| 39 | `Mst_ParamPromotionController` | 4 | 0 | 0 | 4 | 0.0% |
| 40 | `Mst_ParamSysController` | 1 | 1 | 0 | 0 | 100.0% |
| 41 | `Mst_PersonalBackgroundController` | 2 | 0 | 0 | 2 | 0.0% |
| 42 | `Mst_PolicyExpenseTypeController` | 4 | 0 | 0 | 4 | 0.0% |
| 43 | `Mst_PolicyMoneyToPointServiceController` | 3 | 3 | 0 | 0 | 100.0% |
| 44 | `Mst_PositionController` | 8 | 0 | 0 | 8 | 0.0% |
| 45 | `Mst_PromotionProgramController` | 8 | 8 | 0 | 0 | 100.0% |
| 46 | `Mst_ProvinceController` | 9 | 9 | 0 | 0 | 100.0% |
| 47 | `Mst_PurchaseTypeController` | 1 | 1 | 0 | 0 | 100.0% |
| 48 | `Mst_QualificationController` | 2 | 0 | 0 | 2 | 0.0% |
| 49 | `Mst_QualificationFormController` | 2 | 0 | 0 | 2 | 0.0% |
| 50 | `Mst_RankPolicyController` | 4 | 4 | 0 | 0 | 100.0% |
| 51 | `Mst_ReligionCategoryController` | 2 | 0 | 0 | 2 | 0.0% |
| 52 | `Mst_ResignReasonController` | 10 | 0 | 0 | 10 | 0.0% |
| 53 | `Mst_SkillGroupController` | 2 | 0 | 0 | 2 | 0.0% |
| 54 | `Mst_TempPrintTypeController` | 1 | 0 | 0 | 1 | 0.0% |
| 55 | `Mst_WardController` | 2 | 2 | 0 | 0 | 100.0% |
| 56 | `Prm_CarNewController` | 8 | 0 | 0 | 8 | 0.0% |
| 57 | `Prm_CarRecommendController` | 8 | 0 | 0 | 8 | 0.0% |
| 58 | `Prm_VoucherNewCarController` | 9 | 0 | 0 | 9 | 0.0% |
| 59 | `ReqMemberRegisterController` | 9 | 0 | 0 | 9 | 0.0% |
| 60 | `Rpt_MasterDataController` | 2 | 0 | 0 | 2 | 0.0% |
| 61 | `Rpt_PeriodSummaryController` | 14 | 0 | 0 | 14 | 0.0% |
| 62 | `ServicesController` | 2 | 0 | 0 | 2 | 0.0% |
| 63 | `Sys_AccessController` | 7 | 0 | 0 | 7 | 0.0% |
| 64 | `Sys_GroupController` | 10 | 0 | 0 | 10 | 0.0% |
| 65 | `Sys_ObjectController` | 6 | 0 | 0 | 6 | 0.0% |
| 66 | `Sys_UserController` | 14 | 0 | 0 | 14 | 0.0% |
| 67 | `Sys_UserInGroupController` | 1 | 0 | 0 | 1 | 0.0% |
| **Tổng** | **67 Controller** | **337** | **123** | **1** | **213** | **36.50%** |

---

## 5. Phương pháp đo lường & Lệnh kiểm chứng độc lập

Tuân thủ bẫy #22 của `INDEX_CONVERT.md`, mọi con số trong báo cáo này được đo lường bằng lệnh máy tự động:

1. **Đo tổng số mục và phân loại trạng thái**:
   ```powershell
   $lines = Get-Content "C:\Users\Public\Pictures\kientran_private\repos\MiniLoyalty\PARITY_PROGRESS.md" -Encoding utf8
   $rows = $lines | Where-Object { $_ -match '^\|\s*\d+\s*\|' }
   Write-Host "Mẫu số N: $($rows.Count)"
   $rows | ForEach-Object { ($_ -split '\|')[3].Trim() } | Group-Object
   ```
2. **Kiểm chứng số commit hoàn thành trong Git**:
   ```powershell
   $feats = git -C "C:\Users\Public\Pictures\kientran_private\repos\MiniLoyalty" log --grep="feat(parity): port 1-1" --oneline
   Write-Host "Số commit feat(parity): $($feats.Count)"
   ```
3. **Đọc danh mục tạm hoãn**:
   ```powershell
   Get-Content "C:\Users\Public\Pictures\kientran_private\repos\MiniLoyalty\.parity-run\skipped.json" -Raw -Encoding utf8 | ConvertFrom-Json
   ```

---

## 6. Lịch sử cập nhật

| Phiên bản | Ngày cập nhật | Người thực hiện | Nội dung thay đổi |
|---|---|---|---|
| 1.0 | 2026-10-16 11:15 | doc2 (Quản lý quy trình) | Khởi tạo báo cáo tiến độ toàn diện; đo lường độc lập toàn bộ 337 endpoint, 67 controller, 8 nhóm tiền tố và danh mục hoãn. |
| 2.0 | 2026-10-16 12:00 | doc2 (Quản lý quy trình) | Cập nhật tiến độ đợt 2: tăng 8 hạng mục (113 lên 121, đạt 35.91%), hoàn thành dứt điểm Mst_Department (10/10), Mst_FamilyStatus (2/2), Mst_Grade (2/2) và khởi động Mst_GovIDType (3/6). |
| 2.1 | 2026-10-16 12:12 | doc2 (Quản lý quy trình) | Cập nhật số liệu realtime mới nhất từ PARITY_PROGRESS.md: đạt 123/337 (36.50%), bổ sung hoàn thành Mst_GenderController/Search (#154) và Mst_LeaveDayController/Search (#164); nâng số controller có tiến độ lên 27. |
