# PARITY_PROGRESS — port 1-1 `2022.7.Loyalty` → MiniLoyalty

Nguồn mẫu số: `D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers` (mỗi dòng = 1 action public). Sinh bằng `scripts\parity\gen-ledger.ps1` ngày 2026-10-15.

**Mẫu số (N)**: 337

**Tiến độ hiện tại**: 314/337 (93.18%)

Trạng thái: `Chưa làm` · `Đã làm` (có commit, review PASS) · `Đã có` (đã port từ trước, có bằng chứng) · `Bỏ` (scaffold/không nghiệp vụ, ghi lý do) · `Cần xem tay`.

| # | Hạng mục | Trạng thái | Commit |
|---|---|---|---|
| 1 | Crd_CardController/Search | Đã làm | 2660097 |
| 2 | Crd_CardController/GetDetailMember | Đã làm | 9d34ff7 |
| 3 | Crd_CardController/GetDetailMember_New | Đã làm | ae52c1c |
| 4 | Crd_CardController/RequestException | Đã làm | d078153 |
| 5 | Crd_CardController/ApprException | Đã làm | 1025439 |
| 6 | Crd_CardController/Export | Đã làm | 319141b |
| 7 | Crd_CardRankController/Search | Đã làm | 784d0ec |
| 8 | Crd_CardRankController/SearchForMember | Đã làm | 8cc3242 |
| 9 | Crd_CardRankController/ApproveRankDown | Đã làm | 72e9a9e |
| 10 | Crd_CardRankController/ApproveRankKeep | Đã làm | 1872609 |
| 11 | Crd_CardRankController/Export | Đã làm | 8f96c9e |
| 12 | Crd_CardTransactionController/Search | Đã làm | 94c8253 |
| 13 | Crd_CardTransactionController/SearchDetailCrdDealUsePromotion | Đã làm | 66c0351 |
| 14 | Crd_CardTransactionController/Export | Đã làm | 75d4ee0 |
| 15 | Crd_CardTransactionInCreaseController/Search | Đã làm | 9d65ffb |
| 16 | Crd_CardTransactionInCreaseController/Export | Đã làm | d4a9c50 |
| 17 | Crd_CardTransactionServiceTurnController/Search | Cần xem tay | Phu thuoc Crd_DealSerRO/Dtl chua port |
| 18 | Crd_CardTransactionServiceTurnController/Export | Cần xem tay | Phu thuoc #17 |
| 19 | Crd_DealSerROController/Search | Đã làm | 2c23b83 |
| 20 | Crd_DealSerROController/Calc | Cần xem tay | Crd_DealSerRO_AddX ~2400 dong |
| 21 | Crd_DealSerROController/Add | Cần xem tay | Crd_DealSerRO_AddX ~2400 dong |
| 22 | Crd_DealUsePromotionController/Save | Đã làm | cb05505 |
| 23 | Crd_DealUsePromotionController/Search | Đã làm | f31e3e1 |
| 24 | Crd_DealUsePromotionController/GetForMemberNo | Đã làm | e741824 |
| 25 | Crd_DealUsePromotionController/GetByMemberNo | Đã làm | 4516aa0 |
| 26 | Crd_DealUsePromotionController/GetByPhoneNo | Đã làm | 98ea619 |
| 27 | Crd_MemberChangeInfoController/Search | Đã làm | b3374e1 |
| 28 | Crd_MemberChangeInfoController/GetCountNewRequest | Đã làm | ec537f0 |
| 29 | Crd_MemberChangeInfoController/GetByRequestNo | Đã làm | 00efd0a |
| 30 | Crd_MemberChangeInfoController/GetHistoryByMember | Đã làm | e2353bb |
| 31 | Crd_MemberChangeInfoController/GetMemberColumnChange | Đã làm | 13d7b25 |
| 32 | Crd_MemberChangeInfoController/Save | Đã làm | b0f0299 |
| 33 | Crd_MemberChangeInfoController/Add | Đã làm | 89ffbaf |
| 34 | Crd_MemberChangeInfoController/Delete | Đã làm | ea0e47a |
| 35 | Crd_MemberChangeInfoController/Reject | Đã làm | e9be730 |
| 36 | Crd_MemberChangeInfoController/Approve | Đã làm | 017c05b |
| 37 | Crd_MemberChangeInfoController/Finish | Cần xem tay | Phu thuoc Crd_Member_UpdateX_New20230104 + DMS CarSv |
| 38 | Crd_MemberChangeInfoController/Export | Đã làm | 3da4949 |
| 39 | Crd_MemberController/Search | Đã làm | febb049 |
| 40 | Crd_MemberController/GetByMemberNo | Đã làm | 9569b01 |
| 41 | Crd_MemberController/GetByPhoneNo | Đã làm | d4fa21e |
| 42 | Crd_MemberController/GetByCarNo | Đã làm | b3d42fe |
| 43 | Crd_MemberController/GetDetailForDMS | Đã làm | 3e98573 |
| 44 | Crd_MemberController/GetRegisterByMemberNo | Đã làm | 2baa16d |
| 45 | Crd_MemberController/GetCalcPointBuyCreta | Đã làm | 85b280a |
| 46 | Crd_MemberController/Save | Đã làm | 2b9e0b6 |
| 47 | Crd_MemberController/SaveAndApprove | Cần xem tay | Phu thuoc chuoi Finish #53 |
| 48 | Crd_MemberController/Update | Đã làm | 0840dd6 |
| 49 | Crd_MemberController/Delete | Đã làm | f8ef5b6 |
| 50 | Crd_MemberController/Cancel | Đã làm | f8ef5b6 |
| 51 | Crd_MemberController/Approve | Đã làm | f8ef5b6 |
| 52 | Crd_MemberController/ApproveByDealer | Đã làm | f8ef5b6 |
| 53 | Crd_MemberController/Finish | Đã làm | e948a15 |
| 54 | Crd_MemberController/Export | Đã làm | 6968ad3 |
| 55 | Crd_MemberDiscountTransactionController/Search | Đã làm | e152a91 |
| 56 | Crd_MemberVoucherTransactionController/Search | Đã làm | 5a0c9ba |
| 57 | Crd_MemberVoucherTransactionController/Report | Đã làm | 951514c |
| 58 | Crd_MemberVoucherTransactionController/ExportRpt | Đã làm | 2198650 |
| 59 | FileController/UploadFile | Đã làm | 894eb51 |
| 60 | HomeController/Index | Bỏ | Chi return View() (MVC mac dinh) |
| 61 | HomeController/About | Bỏ | Chi return View() (MVC mac dinh) |
| 62 | HomeController/Contact | Bỏ | Chi return View() (MVC mac dinh) |
| 63 | InvF_TempPrintController/GetByTempPrintType | Đã làm | d643394 |
| 64 | InvF_TempPrintController/GetAllActive | Đã làm | 83b1588 |
| 65 | Map_DealerCP_OrgController/GetByDLCPCode | Đã làm | b6bc9a4 |
| 66 | Map_QueryDealer_MemberController/Search | Đã làm | 904a579 |
| 67 | Map_QueryDealer_MemberController/SearchForRePort | Đã làm | f3f9453 |
| 68 | Map_QueryDealer_MemberController/Create | Cần xem tay | Sau luu goi DMS ngoai |
| 69 | Map_QueryDealer_MemberController/ExportExcel | Đã làm | 8c79bfa |
| 70 | Mst_BirthPolicyController/Search | Đã làm | 87e7bc8 |
| 71 | Mst_BirthPolicyController/GetAllActive | Đã làm | 1f45cdd |
| 72 | Mst_BirthPolicyController/GetByBirthPolicyNo | Đã làm | 1177dce |
| 73 | Mst_BirthPolicyController/Save | Đã làm | 41cfecb |
| 74 | Mst_BirthPolicyController/Update | Đã làm | 127bc2b |
| 75 | Mst_BirthPolicyController/Delete | Đã làm | 1eee9a3 |
| 76 | Mst_BirthPolicyController/Export | Đã làm | 7c5d72f |
| 77 | Mst_CarBrandController/Search | Đã làm | 58254a7 |
| 78 | Mst_CarBrandController/GetAllActive | Đã làm | 42bec14 |
| 79 | Mst_CarBrandController/GetByBrandCode | Đã làm | 9f12551 |
| 80 | Mst_CarBrandController/Create | Đã làm | aac1677 |
| 81 | Mst_CarBrandController/Update | Đã làm | 8c9d925 |
| 82 | Mst_CarBrandController/Delete | Đã làm | e2b928d |
| 83 | Mst_CarBrandController/DeleteMultiple | Đã làm | 3be4019 |
| 84 | Mst_CarBrandController/Import | Đã làm | f00ecc2 |
| 85 | Mst_CarBrandController/Export | Đã làm | 451e2e1 |
| 86 | Mst_CarBrandController/ExportTemplate | Đã làm | a75af45 |
| 87 | Mst_CardTypeController/Search | Đã làm | f9f91c1 |
| 88 | Mst_CardTypeController/GetAllActive | Đã làm | 737757f |
| 89 | Mst_CardTypeController/GetByCardType | Đã làm | 8569655 |
| 90 | Mst_CardTypeController/Create | Đã làm | 262abcd |
| 91 | Mst_CardTypeController/Update | Đã làm | 43e777c |
| 92 | Mst_CardTypeController/Delete | Đã làm | 08a9140 |
| 93 | Mst_CardTypeController/DeleteMultiple | Đã làm | 5b4de6e |
| 94 | Mst_CardTypeController/Import | Đã làm | 7d595b1 |
| 95 | Mst_CardTypeController/Export | Đã làm | 934cdcd |
| 96 | Mst_CardTypeController/ExportTemplate | Đã làm | 0bd575c |
| 97 | Mst_CarModelController/Search | Đã làm | 7f59b63 |
| 98 | Mst_CarModelController/GetAllActive | Đã làm | 7a89b1e |
| 99 | Mst_CarModelController/GetByModelCode | Đã làm | fcd66ce |
| 100 | Mst_CarModelController/Create | Đã làm | 52137d6 |
| 101 | Mst_CarModelController/Update | Đã làm | 5685496 |
| 102 | Mst_CarModelController/Delete | Đã làm | 79ced82 |
| 103 | Mst_CarModelController/DeleteMultiple | Đã làm | 5b61bc9 |
| 104 | Mst_CarModelController/Import | Đã làm | c1ef11a |
| 105 | Mst_CarModelController/Export | Đã làm | 847a191 |
| 106 | Mst_CarModelController/ExportTemplate | Đã làm | 1548bea |
| 107 | Mst_CertificateGroupController/Search | Đã làm | d3b52bf |
| 108 | Mst_CertificateGroupController/GetAllActive | Đã làm | 2b9ee3d |
| 109 | Mst_CountryController/GetAllActive | Đã làm | 4d4e166 |
| 110 | Mst_DealerCPController/Search | Đã làm | e57e314 |
| 111 | Mst_DealerCPController/GetAllActive | Đã làm | 3f0c8f9 |
| 112 | Mst_DealerCPController/GetByDLCPCode | Đã làm | 2dc6a66 |
| 113 | Mst_DealerCPController/GetByOrgID | Đã làm | 35489ef |
| 114 | Mst_DealerCPController/CreateOld | Đã làm | 03e9623 |
| 115 | Mst_DealerCPController/Create | Cần xem tay | Phu thuoc Mst_NNT/Sys_User ability |
| 116 | Mst_DealerCPController/Update | Đã làm | 578ef34 |
| 117 | Mst_DealerCPController/Delete | Đã làm | f46f3d7 |
| 118 | Mst_DealerCPController/DeleteMultiple | Đã làm | ccda090 |
| 119 | Mst_DealerCPController/Import | Cần xem tay | Phu thuoc Mst_NNT/Sys_User ability |
| 120 | Mst_DealerCPController/Export | Đã làm | 0b1c1c8 |
| 121 | Mst_DealerCPController/ExportTemplate | Đã làm | 1884d03 |
| 122 | Mst_DepartmentController/Search | Đã làm | ec584e6 |
| 123 | Mst_DepartmentController/GetAllActive | Đã làm | 46644d1 |
| 124 | Mst_DepartmentController/GetByDepartmentCode | Đã làm | 5c1288c |
| 125 | Mst_DepartmentController/Create | Đã làm | 9b5c7cb |
| 126 | Mst_DepartmentController/Update | Đã làm | 7d67b30 |
| 127 | Mst_DepartmentController/Delete | Đã làm | d565dda |
| 128 | Mst_DepartmentController/DeleteMultiple | Đã làm | 9de58bd |
| 129 | Mst_DepartmentController/Import | Đã làm | e3c84ce |
| 130 | Mst_DepartmentController/Export | Đã làm | 2d13be5 |
| 131 | Mst_DepartmentController/ExportTemplate | Đã làm | a2d6f65 |
| 132 | Mst_DisciplineController/Search | Đã làm | 408c2b7 |
| 133 | Mst_DisciplineController/GetByDisciplineCode | Đã làm | 70bd1db |
| 134 | Mst_DisciplineController/GetAllActive | Đã làm | 7a9dcb4 |
| 135 | Mst_DisciplineController/Create | Đã làm | 08defd5 |
| 136 | Mst_DisciplineController/Update | Đã làm | 60ea7cc |
| 137 | Mst_DisciplineController/Delete | Đã làm | bf2220f |
| 138 | Mst_DisciplineController/DeleteMultiple | Đã làm | c71e69b |
| 139 | Mst_DistrictController/Search | Đã làm | 926454e |
| 140 | Mst_DistrictController/GetDetail | Đã làm | 638e1da |
| 141 | Mst_DistrictController/GetByProvinceCode | Đã làm | 4c27394 |
| 142 | Mst_DistrictController/GetAllActive | Đã làm | 1ec6984 |
| 143 | Mst_DistrictController/Create | Đã làm | bcb2a6d |
| 144 | Mst_DistrictController/Update | Đã làm | b956397 |
| 145 | Mst_DistrictController/Delete | Đã làm | 6a0da23 |
| 146 | Mst_DistrictController/Import | Đã làm | 02b9e95 |
| 147 | Mst_DistrictController/Export | Đã làm | e6b27ff |
| 148 | Mst_DistrictController/ExportTemplate | Đã làm | 733b31b |
| 149 | Mst_EthnicCategoryController/Search | Đã làm | 5b9504a |
| 150 | Mst_EthnicCategoryController/GetAllActive | Đã làm | cae436a |
| 151 | Mst_ExpenseTypeController/GetAllActive | Đã làm | aecf24a |
| 152 | Mst_FamilyStatusController/Search | Đã làm | b759701 |
| 153 | Mst_FamilyStatusController/GetAllActive | Đã làm | b22f364 |
| 154 | Mst_GenderController/Search | Đã làm | b8d304e |
| 155 | Mst_GenderController/GetAllActive | Đã làm | eec3343 |
| 156 | Mst_GovIDTypeController/Search | Đã làm | 0cab8a8 |
| 157 | Mst_GovIDTypeController/GetByGovIDType | Đã làm | e05732a |
| 158 | Mst_GovIDTypeController/GetAllActive | Đã làm | fbe0e15 |
| 159 | Mst_GovIDTypeController/Create | Bỏ | Thieu backend: chi gate + dich vu ngoai MstSv |
| 160 | Mst_GovIDTypeController/Update | Bỏ | Thieu backend: chi gate + dich vu ngoai MstSv |
| 161 | Mst_GovIDTypeController/Delete | Bỏ | Thieu backend: chi gate + dich vu ngoai MstSv |
| 162 | Mst_GradeController/Search | Đã làm | 3b851aa |
| 163 | Mst_GradeController/GetAllActive | Đã làm | 6c2dc39 |
| 164 | Mst_LeaveDayController/Search | Đã làm | c6b0984 |
| 165 | Mst_LeaveDayController/GetAllActive | Đã làm | 03b59f5 |
| 166 | Mst_LeaveTypeController/Search | Đã làm | b28baba |
| 167 | Mst_LeaveTypeController/GetAllActive | Đã làm | 39bc3de |
| 168 | Mst_MajorController/Search | Đã làm | c2e0d17 |
| 169 | Mst_MajorController/GetAllActive | Đã làm | b9dfe70 |
| 170 | Mst_MajorGroupController/Search | Đã làm | 2421a22 |
| 171 | Mst_MajorGroupController/GetAllActive | Đã làm | 523031b |
| 172 | Mst_NNTController/Search | Đã làm | 5b52748 |
| 173 | Mst_NNTController/GetAllActive | Đã làm | bbbf3cd |
| 174 | Mst_NNTController/GetByMST | Đã làm | 81877e9 |
| 175 | Mst_NNTController/Create | Đã làm | 0a1e35f |
| 176 | Mst_NNTController/Update | Đã làm | 179f8ab |
| 177 | Mst_NNTController/Delete | Đã làm | a39022d |
| 178 | Mst_OrgController/Search | Đã làm | 775577b |
| 179 | Mst_OrgController/GetAllActive | Đã làm | 84327c2 |
| 180 | Mst_OrgController/GetAllOrg_bk | Bỏ | - |
| 181 | Mst_OrgController/GetAllOrg | Cần xem tay | Phu thuoc iNOS master server |
| 182 | Mst_OrgController/GetOrgsNotInSolution | Cần xem tay | Phu thuoc iNOS master server |
| 183 | Mst_ParamPromotionController/Search | Đã làm | 2972960 |
| 184 | Mst_ParamPromotionController/GetAllActive | Đã làm | 1a3a9bb |
| 185 | Mst_ParamPromotionController/GetByPrProgramCode | Đã làm | 010812f |
| 186 | Mst_ParamPromotionController/Save | Đã làm | 18e34a1 |
| 187 | Mst_ParamSysController/GetAllActive | Đã làm | 2bb249c |
| 188 | Mst_PersonalBackgroundController/Search | Đã làm | 731de3f |
| 189 | Mst_PersonalBackgroundController/GetAllActive | Đã làm | 78b8802 |
| 190 | Mst_PolicyExpenseTypeController/Search | Đã làm | 97d9a6c |
| 191 | Mst_PolicyExpenseTypeController/GetAllActive | Đã làm | 1bb40eb |
| 192 | Mst_PolicyExpenseTypeController/GetByPolicyExpenseTypeNo | Đã làm | 12988f4 |
| 193 | Mst_PolicyExpenseTypeController/Save | Đã làm | 7f37b19 |
| 194 | Mst_PolicyMoneyToPointServiceController/GetAllActive | Đã làm | b7a48c3 |
| 195 | Mst_PolicyMoneyToPointServiceController/GetByPolicyCode | Đã làm | f0df397 |
| 196 | Mst_PolicyMoneyToPointServiceController/Save | Đã làm | 0364844 |
| 197 | Mst_PositionController/Search | Đã làm | 8471c51 |
| 198 | Mst_PositionController/GetByPositionCode | Đã làm | d901bd4 |
| 199 | Mst_PositionController/GetAllActive | Đã làm | 322506e |
| 200 | Mst_PositionController/Create | Đã làm | 35ac6ee |
| 201 | Mst_PositionController/Update | Đã làm | 712e7a9 |
| 202 | Mst_PositionController/Delete | Đã làm | 0ffcb8f |
| 203 | Mst_PositionController/DeleteMultiple | Đã làm | 6db7445 |
| 204 | Mst_PositionController/Export | Đã làm | 6c688df |
| 205 | Mst_PromotionProgramController/Search | Đã làm | dba2d40 |
| 206 | Mst_PromotionProgramController/GetAllActive | Đã làm | 045e8ef |
| 207 | Mst_PromotionProgramController/GetByPrProgramCode | Đã làm | 2b9f2f0 |
| 208 | Mst_PromotionProgramController/Save | Đã làm | c9070f3 |
| 209 | Mst_PromotionProgramController/Update | Đã làm | 26fc5aa |
| 210 | Mst_PromotionProgramController/Delete | Đã làm | 09b4b3d |
| 211 | Mst_PromotionProgramController/Export | Đã làm | e7ebfb9 |
| 212 | Mst_PromotionProgramController/ExportTemplate | Đã làm | 6999fdd |
| 213 | Mst_ProvinceController/Search | Đã làm | 77773a7 |
| 214 | Mst_ProvinceController/GetByProvinceCode | Đã làm | d1a9bb8 |
| 215 | Mst_ProvinceController/GetAllActive | Đã làm | 578190e |
| 216 | Mst_ProvinceController/Create | Đã làm | cfc0f9c |
| 217 | Mst_ProvinceController/Update | Đã làm | bff717b |
| 218 | Mst_ProvinceController/Delete | Đã làm | 901234a |
| 219 | Mst_ProvinceController/Import | Đã làm | 1a50d6b |
| 220 | Mst_ProvinceController/Export | Đã làm | 41f304b |
| 221 | Mst_ProvinceController/ExportTemplate | Đã làm | 47f6ddf |
| 222 | Mst_PurchaseTypeController/GetAllActive | Đã làm | ad8f49a |
| 223 | Mst_QualificationController/Search | Đã làm | e8c6198 |
| 224 | Mst_QualificationController/GetAllActive | Đã làm | d7e35d4 |
| 225 | Mst_QualificationFormController/Search | Đã làm | ef0bc4b |
| 226 | Mst_QualificationFormController/GetAllActive | Đã làm | 0dcd0d4 |
| 227 | Mst_RankPolicyController/Search | Đã làm | b98d2d5 |
| 228 | Mst_RankPolicyController/GetAllActive | Đã làm | a20a029 |
| 229 | Mst_RankPolicyController/GetByRankPolicyCode | Đã làm | cb67c47 |
| 230 | Mst_RankPolicyController/Save | Đã làm | ddbf66a |
| 231 | Mst_ReligionCategoryController/Search | Đã làm | 4665939 |
| 232 | Mst_ReligionCategoryController/GetAllActive | Đã làm | 5761ae3 |
| 233 | Mst_ResignReasonController/Search | Đã làm | 0dd2ff4 |
| 234 | Mst_ResignReasonController/GetByReasonID | Đã làm | 6563718 |
| 235 | Mst_ResignReasonController/GetAllActive | Đã làm | 53b867b |
| 236 | Mst_ResignReasonController/Create | Đã làm | 8c1cc88 |
| 237 | Mst_ResignReasonController/Update | Đã làm | b7b7988 |
| 238 | Mst_ResignReasonController/Delete | Đã làm | de9d222 |
| 239 | Mst_ResignReasonController/DeleteMultiple | Đã làm | 4f08c68 |
| 240 | Mst_ResignReasonController/Import | Đã làm | 2d7d68e |
| 241 | Mst_ResignReasonController/Export | Đã làm | 1cb178c |
| 242 | Mst_ResignReasonController/ExportTemplate | Đã làm | 16d5f4f |
| 243 | Mst_SkillGroupController/Search | Đã làm | 1897e41 |
| 244 | Mst_SkillGroupController/GetAllActive | Đã làm | e7e48e7 |
| 245 | Mst_TempPrintTypeController/GetAllActive | Đã làm | c247f46 |
| 246 | Mst_WardController/GetAllActive | Đã làm | 3aa4d9d |
| 247 | Mst_WardController/GetWardDetail | Đã làm | 4419e9d |
| 248 | Prm_CarNewController/Search | Đã làm | 2c96a7c |
| 249 | Prm_CarNewController/GetByPRMCNCodeSys | Đã làm | 737103f |
| 250 | Prm_CarNewController/Save | Đã làm | e89a6e8 |
| 251 | Prm_CarNewController/Approve | Đã làm | cf6919d |
| 252 | Prm_CarNewController/Finish | Đã làm | 782a7f9 |
| 253 | Prm_CarNewController/Delete | Đã làm | 782a7f9 |
| 254 | Prm_CarNewController/Cancel | Đã làm | 782a7f9 |
| 255 | Prm_CarNewController/CalcPrm | Đã làm | 3e4e75b |
| 256 | Prm_CarRecommendController/Search | Đã làm | f55a618 |
| 257 | Prm_CarRecommendController/GetByPRMCRCodeSys | Đã làm | cc162b6 |
| 258 | Prm_CarRecommendController/Save | Đã làm | 58d1dd4 |
| 259 | Prm_CarRecommendController/Approve | Đã làm | 58d1dd4 |
| 260 | Prm_CarRecommendController/Finish | Đã làm | 58d1dd4 |
| 261 | Prm_CarRecommendController/Delete | Đã làm | 58d1dd4 |
| 262 | Prm_CarRecommendController/Cancel | Đã làm | 58d1dd4 |
| 263 | Prm_CarRecommendController/CalcPrm | Đã làm | 11eab27 |
| 264 | Prm_VoucherNewCarController/Search | Đã làm | b6c2312 |
| 265 | Prm_VoucherNewCarController/GetByPrmVoucherCode | Đã làm | cdabce0 |
| 266 | Prm_VoucherNewCarController/Save | Đã làm | 9bc369d |
| 267 | Prm_VoucherNewCarController/Approve | Đã làm | 9bc369d |
| 268 | Prm_VoucherNewCarController/Finish | Đã làm | 9bc369d |
| 269 | Prm_VoucherNewCarController/Delete | Đã làm | 9bc369d |
| 270 | Prm_VoucherNewCarController/Cancel | Đã làm | 9bc369d |
| 271 | Prm_VoucherNewCarController/CalcPrm | Đã làm | b577eab |
| 272 | Prm_VoucherNewCarController/Export | Đã làm | 8ee4ad9 |
| 273 | ReqMemberRegisterController/Search | Đã làm | 3ea284a |
| 274 | ReqMemberRegisterController/GetByReqMemberRegisterCodeSys | Đã làm | f7b3f4e |
| 275 | ReqMemberRegisterController/GetByReqMemberRegisterCode | Đã làm | e7cdab4 |
| 276 | ReqMemberRegisterController/Export | Đã làm | 3ea284a |
| 277 | ReqMemberRegisterController/Approve | Đã làm | 6ca4719 |
| 278 | ReqMemberRegisterController/ApproveOld | Đã làm | 6ca4719 |
| 279 | ReqMemberRegisterController/Cancel | Đã làm | 6ca4719 |
| 280 | ReqMemberRegisterController/Reject | Đã làm | 6ca4719 |
| 281 | ReqMemberRegisterController/SyncCarSV | Bỏ | Chi dong bo DMS CarSv (dich vu ngoai) |
| 282 | Rpt_MasterDataController/Search | Đã làm | ed607a5 |
| 283 | Rpt_MasterDataController/ExportExcel | Đã làm | 7edce65 |
| 284 | Rpt_PeriodSummaryController/Search | Đã làm | 70325f8 |
| 285 | Rpt_PeriodSummaryController/SearchDetail | Đã làm | f2570fb |
| 286 | Rpt_PeriodSummaryController/SearchDetail01 | Đã làm | cc73773 |
| 287 | Rpt_PeriodSummaryController/SearchTransaction | Đã làm | d6fdf7b |
| 288 | Rpt_PeriodSummaryController/SearchTransactionExceptionally | Đã làm | 3da7de8 |
| 289 | Rpt_PeriodSummaryController/SearchTransactionDetail | Đã làm | e37f2d6 |
| 290 | Rpt_PeriodSummaryController/SearchRptPECrdTransationDeCreaseInstPeriodDtl | Đã làm | c69b6a3 |
| 291 | Rpt_PeriodSummaryController/ExportExcelRpt | Đã làm | eeac12d |
| 292 | Rpt_PeriodSummaryController/ExportExcelRptDtl | Đã làm | 8f754f0 |
| 293 | Rpt_PeriodSummaryController/ExportExcelRptDtl_New20230317 | Đã làm | c41ff84 |
| 294 | Rpt_PeriodSummaryController/ExportExcelTransaction | Đã làm | 6d00d67 |
| 295 | Rpt_PeriodSummaryController/ExportExcelTransactionDetail | Đã làm | 41e59da |
| 296 | Rpt_PeriodSummaryController/ExportExcelTransactionExceptionally | Đã làm | 1cb1454 |
| 297 | Rpt_PeriodSummaryController/PrintPDFRptDtl | Bỏ | Phu thuoc ReportBro server ngoai (PostReport) |
| 298 | ServicesController/GetDLS_DealerCustomer | Bỏ | Chi goi DMS (dich vu ngoai) |
| 299 | ServicesController/GetCarSv_Ser_CustomerCar | Bỏ | Chi goi DMS (dich vu ngoai) |
| 300 | Sys_AccessController/Search | Đã làm | 38aae33 |
| 301 | Sys_AccessController/Get | Đã làm | ad3a9fe |
| 302 | Sys_AccessController/GetByGroupCode | Đã làm | e0454d7 |
| 303 | Sys_AccessController/GetAllFunctionBy_ObjectCode | Đã làm | 425c42f |
| 304 | Sys_AccessController/GetByObjectCode | Đã làm | 1d112a7 |
| 305 | Sys_AccessController/SaveMenuButtonInGroup | Đã làm | b343d2e |
| 306 | Sys_AccessController/SaveFuncInMenu | Đã làm | f740b47 |
| 307 | Sys_GroupController/Search | Đã làm | f557b7d |
| 308 | Sys_GroupController/GetAllActive | Đã làm | 941aa16 |
| 309 | Sys_GroupController/GetByGroupCode | Đã làm | d451746 |
| 310 | Sys_GroupController/Create | Đã làm | d2fea48 |
| 311 | Sys_GroupController/Update | Đã làm | c7b68b6 |
| 312 | Sys_GroupController/Delete | Đã làm | 8611c37 |
| 313 | Sys_GroupController/DeleteMultiple | Đã làm | 4f33f01 |
| 314 | Sys_GroupController/Import | Đã làm | 49dd12b |
| 315 | Sys_GroupController/Export | Đã làm | 80a54fa |
| 316 | Sys_GroupController/ExportTemplate | Đã làm | 43e0e27 |
| 317 | Sys_ObjectController/GetAllMenuActive | Đã làm | 23718cf |
| 318 | Sys_ObjectController/GetAllMenuButtonActive | Đã làm | 3440dc8 |
| 319 | Sys_ObjectController/GetFunctionActive | Đã làm | 4a2db63 |
| 320 | Sys_ObjectController/GetFunctionByObjectCode | Đã làm | 446a639 |
| 321 | Sys_ObjectController/GetAllFunctionBy_ObjectCode | Đã làm | 906a74b |
| 322 | Sys_ObjectController/Get | Đã làm | 0465a58 |
| 323 | Sys_UserController/Search | Đã làm | c99eb93 |
| 324 | Sys_UserController/GetByUserCode | Đã làm | dc6f0cb |
| 325 | Sys_UserController/GetByMST | Đã làm | 06f6450 |
| 326 | Sys_UserController/CheckUserExistAccCenter | Bỏ | Chi goi AccountService ngoai (inos.common) |
| 327 | Sys_UserController/GetForCurrentUser | Đã làm | a92d7c0 |
| 328 | Sys_UserController/GetAllActive | Đã làm | 51f3210 |
| 329 | Sys_UserController/Create | Đã làm | 90f1c7f |
| 330 | Sys_UserController/CreateCP | Đã làm | a5a5e72 |
| 331 | Sys_UserController/Update | Đã làm | 857fd3d |
| 332 | Sys_UserController/Delete | Đã làm | dd1dfe7 |
| 333 | Sys_UserController/DeleteMultiple | Đã làm | b3ee587 |
| 334 | Sys_UserController/Import | Đã làm | 26ebac0 |
| 335 | Sys_UserController/Export | Đã làm | b51c005 |
| 336 | Sys_UserController/ExportTemplate | Đã làm | 831bf2e |
| 337 | Sys_UserInGroupController/Save | Đã làm | c6dddfa |
