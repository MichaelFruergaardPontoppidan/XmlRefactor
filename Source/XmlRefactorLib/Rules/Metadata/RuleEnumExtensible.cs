using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace XmlRefactor
{
    class RuleEnumExtensible : Rule
    {
        public override string RuleName()
        {
            return "Enum -> Extensible Enum";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "Extensibility";
        }
        protected override void buildXpoMatch()
        {            
            xpoMatch.AddXMLStart("AxEnum");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxEnum");
        }
        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        private bool targetName(string name)
        {
        //    if (name != "GanttSetupType")
        //        return false;
            switch (name)
            {
                case "CatCallMethod":
                case "CatContentType":
                case "CatImportStatus":
                case "CatVendorCatalogTemplateCategory":
                case "CatVendorConfigurationForImport":
                case "CatVendorSiteType":
                case "CostingActivationType":
                case "CostWIPStatementCategory":
                case "CustVendTransportPointTypeTransfer":
                case "EcoResProductTemplateType":
                case "ECPsalesOrdersViewType":
                case "EPCSSProductViewType":
                case "EUSalesTransMethod":
                case "GanttSetupType":
                case "GanttTimeUnit":
                case "GanttWrkCtrDisplayColumnsType":
                case "InventCostCostDistribution":
                case "InventItemPriceFilterType":
                case "InventItemPriceType":
                case "InventStdCostPeriodType":
                case "InventTestCorrectionPriority":
                case "InventTestCorrectionStatus":
                case "InventTestQuarantineType":
                case "JmgAbsenceColumnLayout":
                case "JmgBarCodeType":
                case "JmgClockStyle":
                case "JmgControlType":
                case "JmgFeedbackButtonFunction":
                case "JmgFeedbackStyle":
                case "JmgMark":
                case "JmgMessageType":
                case "JmgPostAutomatically":
                case "JmgRegistrationTouchJobStatus":
                case "JmgTermBaudeRate":
                case "JmgTermComPort":
                case "JmgTermDataBit":
                case "JmgTermParity":
                case "JmgTermStopBit":
                case "KanbanBoardType":
                case "KanbanControlActionState":
                case "KanbanControlLegendFormat":
                case "KanbanControlSelectionChanged":
                case "KanbanDemandOriginType":
                case "LeanBOMLineReservationMethod":
                case "PDSAdjustmentPrinciple":
                case "PDSCalcElementBase":
                case "PDSCompensationPrincipleEnum":
                case "PDSElementTypeEnum":
                case "PdsMRCDocumentStatus":
                case "pdsTMAJournalPosting":
                case "PmfOrderTypeFilter":
                case "PmfProdType":
                case "PMFSeqDependency":
                case "PriceSalesPurch":
                case "ProdGanttJobColorType":
                case "ProdGanttLoad":
                case "ProdGanttRouteColorType":
                case "ProdMode":
                case "ProdRefLookUp":
                case "ProdWIPType_NA":
                case "PurchLineBackOrderViews":
                case "PurchReqAutoCreatePurch":
                case "PurchReqCatalogAllNon":
                case "PurchReqConsolidationPriority":
                case "PurchReqReportSortOrder":
                case "PurchReqReportStatus":
                case "PurchReqRFQType":
                case "PurchReqSaveChanges":
                case "PurchReqStatus":
                case "PurchReqWorkflowState":
                case "PurchRFQStatusVendor":
                case "PurchTableMode":
                case "ReqDemPlanForecastType":
                case "ReqGanttColorType":
                case "ReqLevelOrder":
                case "ReqRefTypeTrunc":
                case "ReturnCycleTimeScope":
                case "ReturnReasonCodeDispExtended":
                case "ReturnReasonDispCode":
                case "SalesPurchGroup":
                case "SalesQuotationFilter":
                case "ShipCarrierMkUpFreight":
                case "SMAActiveAll":
                case "SMAAgreementFilter":
                case "SMAAgreementTableListPageType":
                case "SMAServiceOrderFilter":
                case "SMAServiceTaskTitleOption":
                case "smmAppointmentNThInstance":
                case "smmBusinessRelationsListFilter":
                case "smmCampaignProjectJournalType":
                case "smmCampaignsListFilter":
                case "smmContactsListFilter":
                case "smmDisplayEMailInOutlook":
                case "smmDragDropObjectType":
                case "smmFieldDelimiters":
                case "smmLeadsListFilter":
                case "smmQuotationStatus":
                case "smmRecordDelimiters":
                case "smmSaveCopyOfEMail":
                case "smmSwotType":
                case "smmWarningError":
                case "TAMFundType":
                case "TAMPromoCustomerType":
                case "TAMPromotionDate":
                case "TAMRebateCustInclusive":
                case "TAMRebateStatus":
                case "TMSAuditType":
                case "TMSInvoiceAccountType":
                case "TMSTransportationType":
                case "TradePrintType":
                case "VendRequestRoleType":
                case "WHSLoadPlanning":
                case "WHSLoadPostMethodsBase":
                case "WHSLPAssignment":
                case "WHSManifestAt":
                case "WHSMixingLogicTables":
                case "WHSPostMethodBaseKanban":
                case "WHSPostMethodBaseKanbanOptional":
                case "WHSPostMethodBaseOptional":
                case "WHSPostMethodBaseProd":
                case "WHSPostMethodBaseProdOptional":
                case "WHSReservationStatus":
                case "WHSWorkPrintOption":
                case "WMSPalletMovementProcessing":
                case "WrkCtrCommitState":
                case "WrkCtrSchedulerCommand":
                case "WrkCtrSchedulerConstraintType":
                case "WrkCtrSchedulerLoggerMode":
                    return true;
            }
            return false;
        }

        private bool forbiddenName(string name)
        {
            switch (name)
            {
                case "AssetAcceleratedDepDocumentStatus_JP":
                case "AVTimeframeType":
                case "CaseCategoryType":
                case "CaseEntityType":
                case "CaseStatus":
                case "CatExternalCatalogState":
                case "CostingVersionPriceType":
                case "CostSheetNodeType":
                case "CostStatementType":
                case "CustVendorBlocked":
                case "EcoResAttributeModifier":
                case "EcoResCategoryNamedHierarchyRole":
                case "EcoResVariantConfigurationTechnologyType":
                case "FactureLineType_RU":
                case "FactureModule_RU":
                case "FactureType_RU":
                case "InventDirection":
                case "InventProcessRole":
                case "ItemType":
                case "LeanKanbanJobStatus":
                case "LeanKanbanJobType":
                case "LeanKanbanType":
                case "LeanRuleCoverageType":
                case "LedgerTransType":
                case "MarkupModuleType":
                case "ModuleInventPurchSales":
                case "ParmJobStatus":
                case "ProdJobStatus":
                case "ProdStatus":
                case "ProdTransRefType":
                case "ProjEstimateColumn":
                case "ProjPlanVersionType":
                case "ProjStatus":
                case "PurchaseType":
                case "PurchInvoiceType":
                case "PurchRFQStatus":
                case "PurchStatus":
                case "ReqPOType":
                case "ReqRefType":
                case "ResApprovalStatus":
                case "ResCharacteristicReqEntityType":
                case "ResCharacteristicSetEnum":
                case "ResCommitType":
                case "ResReservationType":
                case "RetailAssortmentStatusType":
                case "RetailCDXDownloadSessionStatus":
                case "RetailDeviceActivationStatusBase":
                case "RetailDeviceValidationStatus":
                case "RetailDiscountOfferTypeBase":
                case "RetailEntryStatus":
                case "RetailTransactionType":
                case "ReturnStatusHeader":
                case "RouteJobType":
                case "SalesStatus":
                case "SMADispatched":
                case "smmActivityParentType":
                case "StatusIssue":
                case "StatusReceipt":
                case "TaxOrigin":
                case "TaxTransRelationshipType":
                case "TradeLineDlvType":
                case "VendRequestCompanyStatus":
                case "VersioningDocumentState":
                case "WMSOrderStatus":
                    return true;
            }
            return false;
        }

        public string Run(string input, int startAt = 0)
        {
            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                string xml = input;
                string name = MetaData.extractFromXML(xml, "//AxEnum/Name");
                string isExtensible = MetaData.extractFromXML(xml, "//AxEnum/IsExtensible");
                string useEnumValue = MetaData.extractFromXML(xml, "//AxEnum/UseEnumValue");

                if (useEnumValue == "No" && isExtensible!="true")
                {
                    XmlNodeList nodes = MetaData.extractMultipleFromXML(xml, "//AxEnum/EnumValues/AxEnumValue/Value");
                    int valueExpected = 1;
                    foreach (XmlNode node in nodes)
                    {
                        if (valueExpected != int.Parse(node.InnerText))
                        {
                           Debug.WriteLine(string.Format("{0}", name));
                        }
                        valueExpected++;
                    }
                }
            }

            return input;
        }
    }
}
