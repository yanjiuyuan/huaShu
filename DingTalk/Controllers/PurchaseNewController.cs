using Common.ClassChange;
using Common.DTChange;
using Common.Excel;
using Common.Ionic;
using Common.PDF;
using DingTalk.EF;
using DingTalk.Models;
using DingTalk.Models.DingModels;
using DingTalk.Models.KisLocalModels;
using DingTalk.Models.OfficeLocalModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace DingTalk.Controllers
{
    /// <summary>
    /// 采购(新)
    /// </summary>
    [RoutePrefix("PurchaseNew")]
    public class PurchaseNewController : ApiController
    {
        /// <summary>
        /// 采购表单批量保存
        /// </summary>
        /// <param name="purchaseTableList"></param>
        /// <returns></returns>
        [Route("SavePurchaseTable")]
        [HttpPost]
        public NewErrorModel SavePurchaseTable([FromBody] List<PurchaseTable> purchaseTableList)
        {
            try
            {
                using (DDContext context = new DDContext())
                {
                    foreach (PurchaseTable purchaseTable in purchaseTableList)
                    {
                        context.PurchaseTable.Add(purchaseTable);
                        context.SaveChanges();
                    }
                }
                return new NewErrorModel()
                {
                    error = new Error(0, "保存成功！", "") { },
                };
            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(2, ex.Message, "") { },
                };
            }
        }


        /// <summary>
        /// 采购表单读取
        /// </summary>
        /// <param name="TaskId">流水号</param>
        /// <param name="PurchaseManId">采购员Id名称(可不传)</param>
        /// <returns></returns>
        [Route("ReadPurchaseTable")]
        [HttpGet]
        public NewErrorModel PurseTableRead(string TaskId, string PurchaseManId = "")
        {
            try
            {
                List<PurchaseTable> PurchaseTableList = new List<PurchaseTable>();
                using (DDContext context = new DDContext())
                {
                    if (string.IsNullOrEmpty(PurchaseManId))
                    {
                        PurchaseTableList = context.PurchaseTable.Where
                       (p => p.TaskId == TaskId).ToList();
                    }
                    else
                    {
                        PurchaseTableList = context.PurchaseTable.Where
                      (p => p.TaskId == TaskId && p.PurchaseManId == PurchaseManId).ToList();
                    }

                }
                return new NewErrorModel()
                {
                    data= PurchaseTableList,
                    error = new Error(0, "保存成功！", "") { },
                };
            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(2, ex.Message, "") { },
                };
            }
        }

        /// <summary>
        /// 采购表单批量修改
        /// </summary>
        /// <param name="purchaseTableList"></param>
        /// <returns></returns>
        [Route("ModifyPurchaseTable")]
        [HttpPost]
        public NewErrorModel ModifyPurchaseTable([FromBody] List<PurchaseTable> purchaseTableList)
        {
            try
            {
                using (DDContext context = new DDContext())
                {
                    foreach (PurchaseTable purchaseTable in purchaseTableList)
                    {
                        context.Entry<PurchaseTable>(purchaseTable).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();
                    }
                }
                return new NewErrorModel()
                {
                    error = new Error(0, "修改成功！", "") { },
                };
            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(2, ex.Message, "") { },
                };
            }
        }


        #region 金蝶产品信息读取
        /// <summary>
        /// 金蝶产品信息读取
        /// </summary>
        /// <param name="Key">查询关键字</param>
        /// <returns></returns>
        /// 测试数据 /Purchase/GetICItem?Key=电
        [Route("GetICItem")]
        [HttpGet]
        public NewErrorModel GetICItem(string Key)
        {
            try
            {
                //using (KisContext context = new KisContext())
                //{
                //    var Quary = context.Database.SqlQuery<DingTalk.Models.KisModels.t_ICItem>
                //        (string.Format("SELECT * FROM t_ICItem WHERE FName like  '%{0}%' or  FNumber like '%{1}%'  or FModel  like '%{2}%'", Key, Key, Key)).ToList();

                //    return JsonConvert.SerializeObject(Quary);
                //}
                using (DDContext context = new DDContext())
                {
                    var Quary = context.KisPurchase.Where(k => k.FName.Contains(Key) ||
                    k.FNumber.Contains(Key) || k.FModel.Contains(Key)).ToList();
                    return new NewErrorModel()
                    {
                        data = Quary,
                        error = new Error(0, "读取成功！", "") { },
                    };
                }
            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(2, ex.Message, "") { },
                };
            }
        }

        /// <summary>
        /// 金蝶产品信息读取(手机版分页)
        /// </summary>
        /// <param name="Key">查询关键字</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页容量</param>
        /// <returns></returns>
        [Route("Read")]
        [HttpGet]
        public NewErrorModel Read(string Key, int pageIndex, int pageSize)
        {
            try
            {
                EFHelper<KisPurchase> eFHelper = new EFHelper<KisPurchase>();
                System.Linq.Expressions.Expression<Func<KisPurchase, bool>> expression = null;
                expression = n => n.FName.Contains(Key) || n.FNumber.Contains(Key) || n.FModel.Contains(Key);
                List<KisPurchase> t_ICItemListAll = eFHelper.GetListBy(expression);
                List<KisPurchase> t_ICItem = eFHelper.GetPagedList(pageIndex, pageSize,
                     expression, n => n.FItemID);
                return new NewErrorModel()
                {
                    count = t_ICItemListAll.Count,
                    data = t_ICItem,
                    error = new Error(0, "读取成功！", "") { },
                };
            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(1, ex.Message, "") { },
                };
            }
        }

        /// <summary>
        /// 通过本地数据(内网)同步金蝶数据
        /// </summary>
        /// <param name="State">0表示金蝶物料数据,1表示金蝶办公用品数据</param>
        /// <returns></returns>
        [Route("SynchroData")]
        [HttpGet]
        public object SynchroData(int State)
        {
            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                int count = 0;
                if (State == 0)
                {
                    using (KisLocalContext kisLocalContext = new KisLocalContext())
                    {
                        using (DDContext DDcontext = new DDContext())
                        {
                            List<DingTalk.Models.KisLocalModels.t_ICItem> t_ICItemList = kisLocalContext.t_ICItem.ToList();
                            List<DingTalk.Models.DingModels.KisPurchase> KisPurchaseList = new List<KisPurchase>();
                            //清理旧数据
                            EFHelper<DingTalk.Models.DingModels.KisPurchase> eFHelper = new EFHelper<KisPurchase>();
                            eFHelper.DelBy(k => k.FItemID != null);
                            //构造数据
                            foreach (var item in t_ICItemList)
                            {
                                KisPurchaseList.Add(new KisPurchase()
                                {
                                    FNumber = item.FNumber,
                                    FItemID = item.FItemID.ToString(),
                                    FNote = item.FNote,
                                    FModel = item.FModel,
                                    FName = item.FName
                                });
                            }
                            //批量插入
                            DDcontext.BulkInsert(KisPurchaseList);
                            DDcontext.BulkSaveChanges();
                            count = t_ICItemList.Count();
                        }
                    }
                    watch.Stop();
                    return new NewErrorModel()
                    {
                        count = count,
                        data = "耗时：" + watch.ElapsedMilliseconds,
                        error = new Error(0, "同步成功！", "") { },
                    };
                }
                else
                {
                    using (OfficeLocalContext officeLocalContext = new OfficeLocalContext())
                    {
                        using (DDContext DDcontext = new DDContext())
                        {
                            List<DingTalk.Models.OfficeLocalModels.t_ICItem> t_ICItemList = officeLocalContext.t_ICItem.ToList();
                            List<DingTalk.Models.DingModels.KisOffice> KisOfficeList = new List<KisOffice>();
                            //清理旧数据
                            EFHelper<DingTalk.Models.DingModels.KisOffice> eFHelper = new EFHelper<KisOffice>();
                            eFHelper.DelBy(k => k.FItemID != null);
                            //构造数据
                            foreach (var item in t_ICItemList)
                            {
                                KisOfficeList.Add(new KisOffice()
                                {
                                    FNumber = item.FNumber,
                                    FItemID = item.FItemID.ToString(),
                                    FNote = item.FNote,
                                    FModel = item.FModel,
                                    FName = item.FName
                                });
                            }
                            //批量插入
                            DDcontext.BulkInsert(KisOfficeList);
                            DDcontext.BulkSaveChanges();
                            count = t_ICItemList.Count();
                        }
                    }
                    watch.Stop();
                    return new NewErrorModel()
                    {
                        count = count,
                        data = "耗时：" + watch.ElapsedMilliseconds,
                        error = new Error(0, "同步成功！", "") { },
                    };
                }


            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(2, ex.Message, "") { },
                };
            }
        }

        #endregion


        /// <summary>
        /// 打印表单数据、盖章、推送
        /// </summary>
        /// 测试数据   /Purchase/PrintAndSend
        /// data: { "UserId":"083452125733424957","TaskId":"20"}
        [HttpPost]
        [Route("PrintAndSend")]
        public async Task<NewErrorModel> PrintAndSend([FromBody]PrintAndSendModel printAndSendModel)
        {
            try
            {
                string TaskId = printAndSendModel.TaskId;
                string UserId = printAndSendModel.UserId;
                using (DDContext context = new DDContext())
                {
                    //判断流程是否已结束
                    List<Tasks> tasksList = context.Tasks.Where(t => t.TaskId.ToString() == TaskId && t.State == 0 && t.IsSend == false).ToList();
                    if (tasksList.Count > 0)
                    {
                        return new NewErrorModel()
                        {
                            error = new Error(1, "流程未结束!", "") { },
                        };
                    }


                    List<Roles> roles = context.Roles.Where(r => r.RoleName.Contains("物料采购员")).ToList();

                    //获取表单信息
                    Tasks tasksApplyMan = context.Tasks.Where(t => t.TaskId.ToString() == TaskId && t.NodeId == 0).First();
                    string FlowId = tasksApplyMan.FlowId.ToString();

                    string NodeId = "";
                    if (FlowId == "24")  //零部件
                    {
                        NodeId = "7";
                    }
                    else
                    {
                        NodeId = "6";
                    }

                    List<Tasks> tasks = context.Tasks.Where(t => t.TaskId.ToString() == TaskId && t.NodeId.ToString() == NodeId).ToList();
                    List<Roles> rolesList = new List<Roles>();
                    foreach (var task in tasks)
                    {
                        foreach (var role in roles)
                        {
                            if (task.ApplyManId == role.UserId)
                            {
                                rolesList.Add(role);
                            }
                        }
                    }
                    foreach (var item in rolesList)
                    {
                        await PrintPDF(context, item.UserId, TaskId, UserId);
                    }

                    return new NewErrorModel()
                    {
                        error = new Error(0, "打印推送成功！", "") { },
                    }; 
                }

            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(2, ex.Message, "") { },
                };
            }
        }
        
        public async Task<NewErrorModel> PrintPDF(DDContext context, string ApplyManId, string TaskId, string UserId)
        {
            try
            {
                //获取表单信息
                Tasks tasks = context.Tasks.Where(t => t.TaskId.ToString() == TaskId && t.NodeId == 0).First();
                string FlowId = tasks.FlowId.ToString();
                string ProjectId = tasks.ProjectId;   //项目或合同Id
                PDFHelper pdfHelper = new PDFHelper();
                List<PurchaseTable> purchaseTableList = context.PurchaseTable.Where(u => u.TaskId == TaskId).ToList();
                var SelectPurchaseListOne = from p in purchaseTableList
                                            where p.PurchaseManId == ApplyManId
                                            select new
                                            {
                                                p.CodeNo,
                                                p.Name,
                                                p.Standard,
                                                p.Unit,
                                                p.Count,
                                                p.Purpose,
                                                p.Mark
                                            };

                DataTable dtSourse = DtLinqOperators.CopyToDataTable(SelectPurchaseListOne);
                List<NodeInfo> NodeInfoList = context.NodeInfo.Where(u => u.FlowId == FlowId && u.NodeId != 0 && u.IsSend != true && u.NodeName != "结束").ToList();

                foreach (NodeInfo nodeInfo in NodeInfoList)
                {
                    //临时用作备注
                    nodeInfo.PreNodeId = context.Tasks.Where(t => t.TaskId.ToString() == tasks.TaskId.ToString()
                      && t.NodeId == nodeInfo.NodeId).FirstOrDefault().Remark;

                    if (!string.IsNullOrEmpty(nodeInfo.NodePeople))
                    {
                        if (nodeInfo.NodePeople.Length > 3)
                        {
                            nodeInfo.NodePeople = nodeInfo.NodePeople.Substring(0, 3);
                        }
                    }

                    if (nodeInfo.NodeName.ToString() == "采购员采购")
                    {
                        nodeInfo.NodePeople = "";
                    }
                    if (string.IsNullOrEmpty(nodeInfo.NodePeople))
                    {
                        string strNodePeople = "";
                        string ApplyTime = "";
                        if (nodeInfo.NodeName.ToString() == "采购员采购")
                        {
                            strNodePeople = context.Tasks.Where(q => q.TaskId.ToString() == TaskId && q.NodeId == nodeInfo.NodeId && q.ApplyManId == ApplyManId).First().ApplyMan;
                            ApplyTime = context.Tasks.Where(q => q.TaskId.ToString() == TaskId && q.NodeId == nodeInfo.NodeId && q.ApplyManId == ApplyManId).First().ApplyTime;
                        }
                        else
                        {
                            strNodePeople = context.Tasks.Where(q => q.TaskId.ToString() == TaskId && q.NodeId == nodeInfo.NodeId).First().ApplyMan;
                            ApplyTime = context.Tasks.Where(q => q.TaskId.ToString() == TaskId && q.NodeId == nodeInfo.NodeId).First().ApplyTime;
                        }
                        nodeInfo.NodePeople = strNodePeople + "  " + ApplyTime;
                    }
                    else
                    {
                        string ApplyTime = context.Tasks.Where(q => q.TaskId.ToString() == TaskId && q.NodeId == nodeInfo.NodeId).First().ApplyTime;
                        nodeInfo.NodePeople = nodeInfo.NodePeople + "  " + ApplyTime;
                    }
                }
                DataTable dtApproveView = ClassChangeHelper.ToDataTable(NodeInfoList);
                NodeInfoList.Clear();
                string FlowName = context.Flows.Where(f => f.FlowId.ToString() == FlowId).First().FlowName.ToString();
                string ProjectName = "";
                string ProjectNo = "";
                if (FlowId == "24") //零部件
                {
                    ProjectInfo projectInfo = context.ProjectInfo.Where(p => p.ProjectId == ProjectId).First();
                    ProjectName = projectInfo.ProjectName;
                    ProjectNo = projectInfo.ProjectId;
                }
                else
                {
                    Models.DingModels.Contract contract = context.Contract.Where(p => p.ContractNo == ProjectId).First();
                    ProjectName = contract.ContractName;
                    ProjectNo = contract.ContractNo;
                }



                //绘制BOM表单PDF
                List<string> contentList = new List<string>()
                        {
                            "序号","物料编码","物料名称","规格型号","单位","数量","用途","备注"
                        };

                float[] contentWithList = new float[]
                {
                 50, 60, 60, 100, 40, 60, 40,60
                };
                string Name = "";
                if (FlowId == "24")  //零部件采购
                {
                    Name = "项目";
                }
                else
                {
                    if (FlowId == "26")  //成品采购
                    {
                        Name = "合同";
                    }
                }
                string path = pdfHelper.GeneratePDF(FlowName, TaskId, tasks.ApplyMan, tasks.Dept, tasks.ApplyTime,
                Name, ProjectName, ProjectNo, "2", 300, 650, contentList, contentWithList, dtSourse, dtApproveView, null);
                string RelativePath = "~/UploadFile/PDF/" + Path.GetFileName(path);

                List<string> newPaths = new List<string>();
                RelativePath = AppDomain.CurrentDomain.BaseDirectory + RelativePath.Substring(2, RelativePath.Length - 2).Replace('/', '\\');
                newPaths.Add(RelativePath);
                string SavePath = string.Format(@"{0}\UploadFile\Ionic\{1}.zip", AppDomain.CurrentDomain.BaseDirectory, FlowName + DateTime.Now.ToString("yyyyMMddHHmmss"));
                //文件压缩打包
                IonicHelper.CompressMulti(newPaths, SavePath, false);

                //上传盯盘获取MediaId
                SavePath = string.Format(@"~\UploadFile\Ionic\{0}", Path.GetFileName(SavePath));
                DingTalkServersController dingTalkServersController = new DingTalkServersController();
                var resultUploadMedia = await dingTalkServersController.UploadMedia(SavePath);
                //推送用户
                FileSendModel fileSendModel = JsonConvert.DeserializeObject<FileSendModel>(resultUploadMedia);
                fileSendModel.UserId = UserId;
                var result = await dingTalkServersController.SendFileMessage(fileSendModel);

                return new NewErrorModel()
                {
                    data = result,
                    error = new Error(0, "推送成功！", "") { },
                };
            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(2, ex.Message, "") { },
                };
            }
            
        }


        /// <summary>
        /// 采购管理查询并导出Excel
        /// </summary>
        /// <param name="taskId">流水号</param>
        /// <param name="UserId">用户Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("PrintExcel")]
        public async Task<NewErrorModel> PrintExcel(string taskId, string UserId)
        {
            try
            {
                using (DDContext context = new DDContext())
                {
                    List<PurchaseTable> purchaseTables = context.PurchaseTable.Where(p => p.TaskId == taskId).ToList();
                    //DataTable dtpurchaseTables = ClassChangeHelper.ToDataTable(purchaseTables);
                    var SelectPurchaseList = from p in purchaseTables
                                             select new
                                             {
                                                 p.Id,
                                                 p.TaskId,
                                                 p.CodeNo,
                                                 p.Name,
                                                 Type = p.Standard,
                                                 p.Unit,
                                                 p.Count,
                                                 p.Price,
                                                 p.Purpose,
                                                 p.UrgentDate,
                                                 p.Mark,
                                                 p.SendPosition,
                                                 p.PurchaseMan,
                                                 p.purchaseType
                                             };
                    DataTable dtpurchaseTables = DtLinqOperators.CopyToDataTable(SelectPurchaseList);


                    string path = HttpContext.Current.Server.MapPath("~/UploadFile/Excel/Templet/采购导出模板.xlsx");
                    string time = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string newPath = HttpContext.Current.Server.MapPath("~/UploadFile/Excel/Templet") + "\\采购单" + time + ".xlsx";
                    File.Copy(path, newPath);
                    if (ExcelHelperByNPOI.UpdateExcel(newPath, "Sheet1", dtpurchaseTables, 0, 1))
                    {
                        DingTalkServersController dingTalkServersController = new DingTalkServersController();
                        //上盯盘
                        var resultUploadMedia = await dingTalkServersController.UploadMedia("~/UploadFile/Excel/Templet/采购单" + time + ".xlsx");
                        //推送用户
                        FileSendModel fileSendModel = JsonConvert.DeserializeObject<FileSendModel>(resultUploadMedia);
                        fileSendModel.UserId = UserId;
                        var result = await dingTalkServersController.SendFileMessage(fileSendModel);
                        return new NewErrorModel()
                        {
                            error = new Error(0, result, "Excel已推送至您的钉钉") { },
                        };
                    }
                    else
                    {
                        return new NewErrorModel()
                        {
                            error = new Error(1, "文件有误", "") { },
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    error = new Error(2, ex.Message, "") { },
                };
            }
        }
    }
}
