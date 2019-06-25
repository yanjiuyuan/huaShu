﻿using Common.JsonHelper;
using DingTalk.Bussiness.FlowInfo;
using DingTalk.EF;
using DingTalk.Models;
using DingTalk.Models.DingModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace DingTalk.Controllers
{
    /// <summary>
    /// 工作流通用接口
    /// </summary>
    [RoutePrefix("FlowInfoNew")]
    public class FlowInfoNewController : ApiController
    {
        #region 流程创建与提交、退回

        /// <summary>
        /// 流程创建接口(Post)
        /// </summary>
        /// <param name="taskList"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("CreateTaskInfo")]
        public async Task<NewErrorModel> CreateTaskInfo([FromBody]List<Tasks> taskList)
        {
            try
            {
                FlowInfoServer flowInfoServer = new FlowInfoServer();
                int TaskId = flowInfoServer.FindMaxTaskId();
                #region 新版
                using (DDContext context = new DDContext())
                {
                    int? FlowId = taskList[0].FlowId;
                    foreach (var tasks in taskList)
                    {
                        tasks.TaskId = TaskId;
                        if (tasks.IsSend == true)
                        {
                            tasks.State = 0; tasks.IsEnable = 1;
                            //抄送推送
                            SentCommonMsg(tasks.ApplyManId.ToString(), string.Format("您有一条抄送的流程(流水号:{0})，请及时登入研究院信息管理系统进行审批。", TaskId), taskList[0].ApplyMan, taskList[0].Remark, null);
                        }
                        else
                        {
                            //State 1 表示流程已审批 0 表示未审批  IsEnable 1 表示流程生效  0 未生效
                            if (tasks.NodeId == 0)
                            {
                                tasks.State = 1;
                                tasks.IsEnable = 1;
                                tasks.IsPost = true;
                                tasks.ApplyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            else
                            {
                                tasks.IsPost = false;
                                tasks.State = 0;
                                tasks.IsEnable = 0;
                            }
                        }
                        context.Tasks.Add(tasks);
                        context.SaveChanges();
                    }

                    List<Tasks> tasksNewList = context.Tasks.Where(t => t.TaskId == TaskId).OrderBy(t => t.NodeId).ToList();
                    List<NodeInfo> nodeInfoList = context.NodeInfo.Where(f => f.FlowId == FlowId.ToString()).ToList();
                    //申请人数据
                    Tasks tasksApplyMan = tasksNewList.Where(t => t.NodeId == 0).FirstOrDefault();
                    string PreApplyManId = "";  //上一级处理人

                    int iSendCount = 0;
                    foreach (var tasks in tasksNewList)
                    {
                        PreApplyManId = tasks.ApplyManId;
                        //开始找人
                        string PreNodeId = nodeInfoList.Where(n => n.NodeId == tasks.NodeId + iSendCount).FirstOrDefault().PreNodeId;
                        NodeInfo nextNodeInfo = nodeInfoList.Where(n => n.NodeId.ToString() == PreNodeId).FirstOrDefault();
                        //找到节点表预先配置的人
                        if (!string.IsNullOrEmpty(nextNodeInfo.PeopleId))
                        {
                            string[] PeopleIdList = nextNodeInfo.PeopleId.Split(',');
                            string[] NodePeopleList = nextNodeInfo.NodePeople.Split(',');
                            if (nextNodeInfo.IsSend != true)  //非抄送
                            {
                                //遍历推送消息
                                for (int i = 0; i < PeopleIdList.Length; i++)
                                {
                                    Tasks Newtasks = tasks;
                                    Newtasks.Remark = "";
                                    Newtasks.NodeId = int.Parse(PreNodeId);
                                    Newtasks.ApplyMan = NodePeopleList[i];
                                    Newtasks.ApplyManId = PeopleIdList[i];
                                    Newtasks.ApplyTime = null;
                                    tasks.IsEnable = 1;
                                    tasks.State = 0;
                                    Newtasks.IsPost = false;
                                    context.Tasks.Add(Newtasks);
                                    context.SaveChanges();

                                    await SendOaMsgNew(tasks.FlowId, PeopleIdList[i].ToString(),
                                        TaskId.ToString(), tasksApplyMan.ApplyMan,
                                        tasksApplyMan.Remark, context, false, false);
                                    Thread.Sleep(500);
                                }

                                return new NewErrorModel()
                                {
                                    data = TaskId.ToString(),
                                    error = new Error(0, "流程创建成功！", "") { },
                                };
                            }
                            else  //找到抄送
                            {
                                //遍历推送消息
                                iSendCount--;
                                for (int i = 0; i < PeopleIdList.Length; i++)
                                {
                                    Tasks Newtasks = tasks;
                                    Newtasks.IsPost = false;
                                    Newtasks.IsSend = true;
                                    Newtasks.Remark = "";
                                    Newtasks.NodeId = int.Parse(PreNodeId);
                                    Newtasks.ApplyMan = NodePeopleList[i];
                                    Newtasks.ApplyManId = PeopleIdList[i];
                                    Newtasks.ApplyTime = "";
                                    tasks.IsEnable = 1;
                                    tasks.State = 0;
                                    context.Tasks.Add(Newtasks);
                                    context.SaveChanges();

                                    await SendOaMsgNew(tasks.FlowId, PeopleIdList[i].ToString(),
                                        TaskId.ToString(), tasksApplyMan.ApplyMan,
                                        tasksApplyMan.Remark, context, false, true);
                                    Thread.Sleep(500);

                                    //特殊处理(暂时)
                                    if (tasks.FlowId.ToString() == "6")
                                    {
                                        NodeInfo nodeInfoCurrent = context.NodeInfo.Where(n => n.FlowId.ToString() == "6" && n.NodeId.ToString() == "2").FirstOrDefault();
                                        Tasks taskCurrent = new Tasks()
                                        {
                                            TaskId = tasks.TaskId,
                                            ApplyMan = nodeInfoCurrent.NodePeople,
                                            ApplyManId = nodeInfoCurrent.PeopleId,
                                            IsPost = false,
                                            State = 0,
                                            IsSend = false,
                                            NodeId = 2,
                                            IsEnable = 1,
                                            FlowId = 6
                                        };
                                        context.Tasks.Add(taskCurrent);
                                        context.SaveChanges();
                                        await SendOaMsgNew(tasks.FlowId, taskCurrent.ApplyManId,
                                            TaskId.ToString(), tasksApplyMan.ApplyMan,
                                            tasksApplyMan.Remark, context);
                                        Thread.Sleep(500);

                                        return new NewErrorModel()
                                        {
                                            data = TaskId.ToString(),
                                            error = new Error(0, "流程创建成功！", "") { },
                                        };
                                    }
                                }
                            }
                        }
                        else  //节点表数据未找到人
                        {
                            //找到已选人数据
                            List<Tasks> tasksChoosedList = tasksNewList.Where(t => t.NodeId.ToString() == PreNodeId).OrderBy(t => t.NodeId).ToList();
                            foreach (var tasksChoosed in tasksChoosedList)
                            {
                                //与上一级处理人重复
                                if (tasksChoosed.ApplyManId == PreApplyManId && iSendCount == 0
                                    && tasksChoosed.FlowId.ToString() != "26" && tasksChoosed.FlowId.ToString() != "27")  //临时处理
                                {
                                    tasksChoosed.ApplyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    tasksChoosed.State = 1; //修改审批状态
                                    tasksChoosed.IsEnable = 1; //修改显示状态
                                    context.Entry<Tasks>(tasksChoosed).State = EntityState.Modified;
                                    context.SaveChanges();
                                }
                                else
                                {
                                    PreApplyManId = tasksChoosed.ApplyManId;
                                    //修改显示状态
                                    tasksChoosed.IsEnable = 1;
                                    context.Entry<Tasks>(tasksChoosed).State = EntityState.Modified;
                                    context.SaveChanges();

                                    await SendOaMsgNew(tasks.FlowId, tasksChoosed.ApplyManId.ToString(),
                                        TaskId.ToString(), tasksApplyMan.ApplyMan,
                                        tasksApplyMan.Remark, context, false, false);
                                    Thread.Sleep(500);
                                    //推送OA消息
                                    //SentCommonMsg(tasksChoosed.ApplyManId.ToString(), string.Format("您有一条待审批的流程(流水号:{0})，请及时登入研究院信息管理系统进行审批。", TaskId), tasksApplyMan.ApplyMan, tasksApplyMan.Remark, null);

                                    return new NewErrorModel()
                                    {
                                        data = TaskId.ToString(),
                                        error = new Error(0, "流程创建成功！", "") { },
                                    };
                                }
                            }
                        }
                    }
                }

                #endregion

                return new NewErrorModel()
                {
                    data = TaskId.ToString(),
                    error = new Error(0, "流程创建成功！", "") { },
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
        /// 流程提交接口(Approve)
        /// </summary>
        /// <param name="taskList"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("SubmitTaskInfo")]
        public async Task<NewErrorModel> SubmitTaskInfo([FromBody]List<Tasks> taskList)
        {
            try
            {
                //获取申请人提交表单信息
                FlowInfoServer fServer = new FlowInfoServer();
                Tasks taskNew = fServer.GetApplyManFormInfo(taskList[0].TaskId.ToString());

                if (taskList.Count > 1)  //如果有选人
                {
                    using (DDContext contexts = new DDContext())
                    {
                        foreach (var task in taskList)
                        {
                            if (taskList.IndexOf(task) > 0)
                            {
                                if (task.IsSend == true)
                                {
                                    //推送抄送消息
                                    SentCommonMsg(task.ApplyManId,
                                    string.Format("您有一条抄送信息(流水号:{0})，请及时登入研究院信息管理系统进行查阅。", task.TaskId),
                                    taskNew.ApplyMan, taskNew.Remark, null);
                                    task.IsEnable = 1;
                                }
                                else
                                {
                                    task.IsEnable = 0;
                                }
                                contexts.Tasks.Add(task);
                                contexts.SaveChanges();
                            }
                        }
                    }
                }

                //调用寻人接口
                Tasks Findtasks = taskList[0];
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic = FindNextPeople(Findtasks.FlowId.ToString(), Findtasks.ApplyManId, true, Findtasks.IsSend,
                Findtasks.TaskId, Findtasks.NodeId);
                int i = 1; //控制推送次数

                foreach (var tasks in taskList)
                {
                    using (DDContext context = new DDContext())
                    {
                        if (dic["NodeName"] == "结束")
                        {
                            //修改流程状态
                            tasks.IsPost = false;
                            tasks.State = 1;
                            tasks.ApplyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            context.Entry(tasks).State = EntityState.Modified;
                            context.SaveChanges();

                            Tasks tasksApplyMan = context.Tasks.Where(t => t.TaskId.ToString() == tasks.TaskId.ToString()
                              && t.NodeId == 0).First();
                            tasksApplyMan.ImageUrl = tasks.ImageUrl;
                            tasksApplyMan.OldImageUrl = tasks.OldImageUrl;
                            tasksApplyMan.ImageUrl = tasks.ImageUrl;
                            if (!string.IsNullOrEmpty(tasksApplyMan.FileUrl))
                            {
                                if (!string.IsNullOrEmpty(tasks.FileUrl))
                                {
                                    tasksApplyMan.FileUrl = tasksApplyMan.FileUrl + "," + tasks.FileUrl;
                                    tasksApplyMan.OldFileUrl = tasksApplyMan.OldFileUrl + "," + tasks.OldFileUrl;
                                    tasksApplyMan.MediaId = tasksApplyMan.MediaId + "," + tasks.MediaId;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(tasks.FileUrl))
                                {
                                    tasksApplyMan.FileUrl = tasks.FileUrl;
                                    tasksApplyMan.OldFileUrl = tasks.OldFileUrl;
                                    tasksApplyMan.MediaId = tasks.MediaId;
                                }
                            }
                            context.Entry(tasksApplyMan).State = EntityState.Modified;
                            context.SaveChanges();


                            //推送发起人
                            SentCommonMsg(taskNew.ApplyManId,
                            string.Format("您发起的审批的流程(流水号:{0})，已审批完成请知晓。", tasks.TaskId),
                            taskNew.ApplyMan, taskNew.Remark, null);

                            JsonConvert.SerializeObject(new ErrorModel
                            {
                                errorCode = 0,
                                errorMessage = "流程结束",
                                Content = tasks.TaskId.ToString()
                            });
                        }
                        else
                        {
                            if (taskList.IndexOf(tasks) == 0)
                            {
                                //修改流程状态
                                tasks.IsPost = false;
                                tasks.State = 1;
                                tasks.ApplyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                context.Entry(tasks).State = EntityState.Modified;
                                context.SaveChanges();

                                Tasks tasksApplyMan = context.Tasks.Where(t => t.TaskId.ToString() == tasks.TaskId.ToString()
                                && t.NodeId == 0).First();
                                tasksApplyMan.ImageUrl = tasks.ImageUrl;
                                tasksApplyMan.OldImageUrl = tasks.OldImageUrl;
                                tasksApplyMan.ImageUrl = tasks.ImageUrl;
                                if (!string.IsNullOrEmpty(tasksApplyMan.FileUrl))
                                {
                                    if (!string.IsNullOrEmpty(tasks.FileUrl))
                                    {
                                        tasksApplyMan.FileUrl = tasksApplyMan.FileUrl + "," + tasks.FileUrl;
                                        tasksApplyMan.OldFileUrl = tasksApplyMan.OldFileUrl + "," + tasks.OldFileUrl;
                                        tasksApplyMan.MediaId = tasksApplyMan.MediaId + "," + tasks.MediaId;
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(tasks.FileUrl))
                                    {
                                        tasksApplyMan.FileUrl = tasks.FileUrl;
                                        tasksApplyMan.OldFileUrl = tasks.OldFileUrl;
                                        tasksApplyMan.MediaId = tasks.MediaId;
                                    }
                                }
                                context.Entry(tasksApplyMan).State = EntityState.Modified;
                                context.SaveChanges();
                            }
                            else
                            {
                                //创建流程推送(选人)
                                //tasks.IsPost = false;
                                //tasks.State = 0;
                                //context.Tasks.Add(tasks);
                                //context.SaveChanges();
                            }
                            if (taskList.Count == 1 && taskList.IndexOf(tasks) == 0)  //未选人
                            {
                                //当前节点所有任务流已完成
                                if (fServer.GetTasksByNotFinished(tasks.TaskId.ToString(), tasks.NodeId.ToString()).Count == 0)
                                {
                                    //推送OA消息(寻人)
                                    //SentCommonMsg(dic["PeopleId"].ToString(),
                                    //string.Format("您有一条待审批的流程(流水号:{0})，请及时登入研究院信息管理系统进行审批。", tasks.TaskId),
                                    //taskNew.ApplyMan, taskNew.Remark, null);

                                    await SendOaMsgNew(tasks.FlowId, dic["PeopleId"].ToString(), tasks.TaskId.ToString(),
                                        taskNew.ApplyMan, taskNew.Remark, context, false, false);
                                    Thread.Sleep(500);
                                }
                            }
                            else
                            {
                                if (dic["PeopleId"] != null)
                                {
                                    if (i == 1)  //防止重复推送
                                    {
                                        //推送OA消息
                                        string[] PeopleIdList = dic["PeopleId"].Split(',');
                                        foreach (var PeopleId in PeopleIdList)
                                        {
                                            await SendOaMsgNew(tasks.FlowId, PeopleId, tasks.TaskId.ToString(),
                                                taskNew.ApplyMan, taskNew.Remark, context, false, false);
                                            Thread.Sleep(500);
                                            //     SentCommonMsg(PeopleId,
                                            //string.Format("您有一条待审批的流程(流水号:{0})，请及时登入研究院信息管理系统进行审批。", tasks.TaskId),
                                            //taskNew.ApplyMan, taskNew.Remark, null);
                                        }
                                        i++;
                                    }
                                }
                            }
                        }
                    }
                }
                return new NewErrorModel()
                {
                    error = new Error(0, "流程创建成功！", "") { },
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
        /// 流程退回
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("FlowBack")]
        public async Task<NewErrorModel> FlowBack(Tasks tasks)
        {
            try
            {
                DDContext context = new DDContext();
                if (tasks.NodeId == 0)  //撤回
                {
                    Tasks taskNew = context.Tasks.Find(tasks.Id);
                    taskNew.IsBacked = true;
                    context.Entry<Tasks>(taskNew).State = EntityState.Modified;
                    context.SaveChanges();
                    //找到当前未审核的人员修改状态
                    List<Tasks> taskList = context.Tasks.Where(t => t.TaskId.ToString() == tasks.TaskId.ToString() && t.State == 0 && t.IsSend != true).ToList();
                    foreach (var task in taskList)
                    {
                        context.Tasks.Remove(task);
                        context.SaveChanges();
                    }

                    return new NewErrorModel()
                    {
                        data = tasks.TaskId.ToString(),
                        error = new Error(0, "流程撤回成功！", "") { },
                    };

                }
                else
                {
                    //修改流程状态
                    tasks.State = 1;
                    tasks.IsBacked = true;
                    tasks.ApplyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    context.Entry(tasks).State = EntityState.Modified;
                    context.SaveChanges();
                    //查找退回节点Id
                    string newBackNodeId = context.NodeInfo.Where
                        (u => u.FlowId == tasks.FlowId.ToString() && u.NodeId == tasks.NodeId)
                        .Select(u => u.BackNodeId).First();
                    //根据退回节点Id找人
                    if (newBackNodeId == "0")  //退回节点为发起人
                    {
                        Tasks taskApplyMan = context.Tasks.Where(t => t.TaskId.ToString() == tasks.TaskId.ToString() && t.NodeId == 0).First();
                        await SendOaMsgNew(tasks.FlowId, taskApplyMan.ApplyManId, tasks.TaskId.ToString(),
                            taskApplyMan.ApplyMan, tasks.Remark, context, true, false);
                        Thread.Sleep(500);
                    }
                    else
                    {
                        string PeopleId = context.NodeInfo.SingleOrDefault
                            (u => u.NodeId.ToString() == newBackNodeId && u.FlowId == tasks.FlowId.ToString()).PeopleId;
                        string NodePeople = context.NodeInfo.SingleOrDefault
                            (u => u.NodeId.ToString() == newBackNodeId && u.FlowId == tasks.FlowId.ToString()).NodePeople;
                        if (string.IsNullOrEmpty(PeopleId))
                        {
                            return new NewErrorModel()
                            {
                                data = tasks.TaskId.ToString(),
                                error = new Error(2, "退回节点尚未配置人员！", "") { },
                            };
                        }
                        else
                        {
                            int iBackNodeIds = int.Parse(newBackNodeId);
                            //根据找到的人创建新任务流
                            Tasks newTask = new Tasks();
                            newTask = tasks;
                            newTask.IsBacked = false;
                            newTask.ApplyMan = NodePeople;
                            newTask.ApplyManId = PeopleId;
                            newTask.ApplyTime = null;
                            newTask.State = 0;
                            newTask.NodeId = iBackNodeIds;
                            newTask.Remark = null;
                            newTask.IsPost = false;
                            context.Tasks.Add(newTask);
                            context.SaveChanges();
                        }
                    }
                }

                return new NewErrorModel()
                {
                    data = tasks.TaskId.ToString(),
                    error = new Error(0, "退回成功！", "") { },
                };
            }
            catch (Exception ex)
            {
                return new NewErrorModel()
                {
                    data = tasks.TaskId.ToString(),
                    error = new Error(1, ex.Message, "") { },
                };
            }
        }

        #endregion

        #region 审批过程节点数据读取

        /// <summary>
        /// 审批过程节点数据读取接口
        /// </summary>
        /// <param name="TaskId">流水号</param>
        /// <param name="FlowId">流程号</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetFlowProgress")]
        public NewErrorModel GetFlowProgress(string TaskId, string FlowId)
        {
            try
            {
                using (DDContext context = new DDContext())
                {
                    if (string.IsNullOrEmpty(FlowId))
                    {
                        return new NewErrorModel()
                        {
                            error = new Error(1, "FlowId不能为空！", "") { },
                        };
                    }
                    if (string.IsNullOrEmpty(TaskId))
                    {
                        var QuaryList = context.NodeInfo.Where(u => u.FlowId == FlowId)
                            .Select(T => new
                            {
                                NodeId = T.NodeId,
                                NodeName = T.NodeName,
                                NodePeople = T.NodePeople
                            }).OrderBy(t => t.NodeId);

                        return new NewErrorModel()
                        {
                            data = QuaryList,
                            error = new Error(0, "读取成功！", "") { },
                        };
                    }
                    else
                    {
                        var TasksList = context.Tasks.Where(u => u.TaskId.ToString() == TaskId && u.FlowId.ToString() == FlowId);
                        var NodeInfoList = context.NodeInfo.Where(u => u.FlowId == FlowId);
                        var QuaryList = from a in TasksList
                                        join b in NodeInfoList
                                        on a.NodeId equals b.NodeId
                                        orderby b.NodeId ascending
                                        select new
                                        {
                                            NodeId = a.NodeId,
                                            NodeName = b.NodeName,
                                            NodePeople = b.NodePeople,
                                            ApplyTime = a.ApplyTime,
                                            ApplyMan = a.ApplyMan,
                                            IsSend = a.IsSend
                                        };

                        return new NewErrorModel()
                        {
                            data = QuaryList,
                            error = new Error(0, "读取成功！", "") { },
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

        #endregion

        #region 寻人、选人与抄送

        /// <summary>
        /// 寻人接口（默认）
        /// </summary>
        /// <param name="FlowId">流程Id</param>
        /// <param name="ApplyManId">提交人Id</param>
        /// <param name="IsNext">是否找下一个</param>
        /// <param name="IsSend">是否抄送</param>
        /// <param name="OldTaskId">旧流水号</param>
        /// <param name="NodeId">当前节点Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("FindNextPeople")]
        public Dictionary<string, string> FindNextPeople(string FlowId, string ApplyManId, bool IsNext = true,
            bool? IsSend = false, int? OldTaskId = 0, int? NodeId = -1)
        {
            using (DDContext context = new DDContext())
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                string FindNodeId = context.NodeInfo.SingleOrDefault(u => u.FlowId == FlowId && u.NodeId == NodeId).PreNodeId;
                string NodeName = context.NodeInfo.SingleOrDefault(u => u.FlowId == FlowId && u.NodeId.ToString() == FindNodeId).NodeName;
                dic.Add("NodeName", NodeName);
                if (NodeName == "结束")
                {
                    return dic;
                }
                string PeopleId = context.NodeInfo.SingleOrDefault(u => u.FlowId == FlowId && u.NodeId.ToString() == FindNodeId).PeopleId;
                string NodePeople = context.NodeInfo.SingleOrDefault(u => u.FlowId == FlowId && u.NodeId.ToString() == FindNodeId).NodePeople;
                bool? IsNeedChose = context.NodeInfo.SingleOrDefault(u => u.FlowId == FlowId && u.NodeId.ToString() == FindNodeId).IsNeedChose;
                //判断流程多人提交(当前步骤)
                bool? IsAllAllow = context.NodeInfo.Where(u => u.NodeId == NodeId && u.FlowId == FlowId).First().IsAllAllow;
                dic.Add("NodePeople", NodePeople);
                dic.Add("PeopleId", PeopleId);

                if (NodeName == "抄送")
                {
                    string[] ListNodeName = NodeName.Split(',');
                    string[] ListPeopleId = PeopleId.Split(',');
                    string[] ListNodePeople = NodePeople.Split(',');

                    Tasks Task = context.Tasks.Where(u => u.TaskId == OldTaskId).First();
                    for (int i = 0; i < ListPeopleId.Length; i++)
                    {
                        //保存任务流
                        Tasks newTask = new Tasks()
                        {
                            TaskId = OldTaskId,
                            ApplyMan = ListNodePeople[i],
                            IsEnable = 1,
                            NodeId = NodeId + 1,
                            FlowId = Int32.Parse(FlowId),
                            IsSend = true,
                            ApplyManId = ListPeopleId[i],
                            State = 0, //0 表示未审核 1表示已审核
                            FileUrl = Task.FileUrl,
                            OldFileUrl = Task.OldFileUrl,
                            ImageUrl = Task.ImageUrl,
                            OldImageUrl = Task.OldImageUrl,
                            Title = Task.Title,
                            IsPost = false,
                            ProjectId = Task.ProjectId,
                        };

                        //推送抄送消息
                        SentCommonMsg(ListPeopleId[i],
                        string.Format("您有一条抄送信息(流水号:{0})，请及时登入研究院信息管理系统进行查阅。", Task.TaskId),
                        Task.ApplyMan, Task.Remark, null);

                        context.Tasks.Add(newTask);
                        context.SaveChanges();
                    }
                    if (IsSend == true)
                    {
                        //return FindNextPeople(FlowId, ApplyManId, true, false, OldTaskId, NodeId + 2);
                        return null;
                    }
                    else
                    {
                        return FindNextPeople(FlowId, ApplyManId, true, false, OldTaskId, NodeId + 1);
                    }
                }
                //查找当前是否还有人未审核
                List<Tasks> ListTask = context.Tasks.Where(u => u.TaskId == OldTaskId && u.FlowId.ToString() == FlowId && u.NodeId == NodeId && u.NodeId != 0 && u.ApplyManId != ApplyManId && u.State == 0 && u.IsSend != true).ToList();
                if (ListTask.Count > 0)  //还有人未审核
                {
                    return dic;
                }
                else
                {
                    if (NodeName == "抄送相应部门部长")
                    {
                        return FindNextPeople(FlowId, ApplyManId, true, false, OldTaskId, NodeId + 1);
                    }
                    if (NodeName == "抄送所有人")
                    {
                        List<Tasks> TasksList = context.Tasks.Where(t => t.TaskId == OldTaskId).ToList();
                        //context.Tasks.Where(t => t.TaskId == OldTaskId).Select(h => h.ApplyManId).Distinct().ToList();
                        List<string> AppplyManIdList = new List<string>();
                        foreach (var task in TasksList)
                        {
                            if (!AppplyManIdList.Contains(task.ApplyManId))
                            {
                                AppplyManIdList.Add(task.ApplyManId);
                                task.IsSend = true;
                                task.ApplyTime = "";
                                task.Remark = "";
                                task.NodeId = NodeId + 1;
                                task.IsEnable = 1;
                                task.State = 0;
                                context.Tasks.Add(task);
                                context.SaveChanges();
                            }
                            //推送抄送消息
                            Tasks Task = context.Tasks.Where(u => u.TaskId == OldTaskId).First();
                            SentCommonMsg(task.ApplyManId,
                            string.Format("您有一条抄送信息(流水号:{0})，请及时登入研究院信息管理系统进行查阅。", Task.TaskId),
                            Task.ApplyMan, Task.Remark, null);
                        }
                        return FindNextPeople(FlowId, ApplyManId, true, false, OldTaskId, NodeId + 1);
                    }
                }


                //节点表找不到人，任务表找
                if (string.IsNullOrEmpty(NodePeople) && string.IsNullOrEmpty(PeopleId))
                {
                    string PreNodeId = context.NodeInfo.Where(f => f.NodeId == NodeId && f.FlowId == FlowId).First().PreNodeId;
                    List<Tasks> taskList = context.Tasks.Where(t => t.TaskId == OldTaskId && t.NodeId.ToString() == PreNodeId).ToList();
                    int iCount = 0;
                    foreach (var tasks in taskList)
                    {
                        if (tasks.IsSend == false)  //非抄送
                        {
                            //查找当前是否还有人未审核
                            if (ListTask.Count > 0)  //还有人未审核
                            {
                                return dic;
                            }
                            if (IsAllAllow != true)  //逐一审核
                            {
                                if (iCount == 0)
                                {
                                    dic["PeopleId"] = tasks.ApplyManId;
                                    dic["NodePeople"] = tasks.ApplyMan;
                                    tasks.IsEnable = 1;
                                }
                                else
                                {
                                    tasks.IsEnable = 0;
                                }
                                iCount++;
                            }
                            else  //同时审核
                            {
                                if (iCount == 0)
                                {
                                    iCount++;
                                    dic["PeopleId"] = tasks.ApplyManId;
                                    dic["NodePeople"] = tasks.ApplyMan;
                                    tasks.IsEnable = 1;
                                }
                                else
                                {
                                    dic["PeopleId"] += "," + tasks.ApplyManId;
                                    dic["NodePeople"] += "," + tasks.ApplyMan;
                                    tasks.IsEnable = 1;
                                }
                            }
                            context.Entry<Tasks>(tasks).State = EntityState.Modified;
                            context.SaveChanges();
                        }
                        else
                        {
                            return FindNextPeople(FlowId, ApplyManId, true, false, OldTaskId, Int32.Parse(FindNodeId) + 1);
                        }
                    }
                    return dic;
                }

                if (PeopleId == null && IsNeedChose == false)  //找不到人、且不需要找人时继续查找下一节点人员
                {
                    return FindNextPeople(FlowId, ApplyManId, true, false, OldTaskId, NodeId + 1);
                }
                else
                {
                    if (IsAllAllow == true)   //流程配置为所有人同时同意后提交
                    {
                        //查找当前是否还有人未审核
                        if (ListTask.Count > 0)  //还有人未审核
                        {
                            return dic;
                        }
                        else
                        {
                            string[] ListNodeName = NodeName.Split(',');
                            string[] ListPeopleId = PeopleId.Split(',');
                            string[] ListNodePeople = NodePeople.Split(',');

                            Tasks Task = context.Tasks.Where(u => u.TaskId == OldTaskId).First();
                            for (int i = 0; i < ListPeopleId.Length; i++)
                            {
                                //保存任务流
                                Tasks newTask = new Tasks()
                                {
                                    TaskId = OldTaskId,
                                    ApplyMan = ListNodePeople[i],
                                    IsEnable = 1,
                                    NodeId = NodeId + 1,
                                    FlowId = Int32.Parse(FlowId),
                                    IsSend = IsSend,
                                    ApplyManId = ListPeopleId[i],
                                    State = 0, //0 表示未审核 1表示已审核
                                    FileUrl = Task.FileUrl,
                                    OldFileUrl = Task.OldFileUrl,
                                    ImageUrl = Task.ImageUrl,
                                    OldImageUrl = Task.OldImageUrl,
                                    Title = Task.Title,
                                    IsPost = false,
                                    ProjectId = Task.ProjectId,
                                };
                                context.Tasks.Add(newTask);
                            }
                            context.SaveChanges();
                        }
                    }
                    else  //流程配置为任意一人同意后提交
                    {
                        string[] ListNodeName = NodeName.Split(',');
                        string[] ListPeopleId = PeopleId.Split(',');
                        string[] ListNodePeople = NodePeople.Split(',');
                        Tasks Task = context.Tasks.Where(u => u.TaskId == OldTaskId).First();
                        for (int i = 0; i < ListPeopleId.Length; i++)
                        {
                            //保存任务流
                            Tasks newTask = new Tasks()
                            {
                                TaskId = OldTaskId,
                                ApplyMan = ListNodePeople[i],
                                IsEnable = 1,
                                NodeId = NodeId + 1,
                                FlowId = Int32.Parse(FlowId),
                                IsSend = IsSend,
                                ApplyManId = ListPeopleId[i],
                                State = 0, //0 表示未审核 1表示已审核
                                FileUrl = Task.FileUrl,
                                OldFileUrl = Task.OldFileUrl,
                                ImageUrl = Task.ImageUrl,
                                OldImageUrl = Task.OldImageUrl,
                                Title = Task.Title,
                                IsPost = false,
                                ProjectId = Task.ProjectId,
                            };
                            context.Tasks.Add(newTask);
                        }
                        context.SaveChanges();
                    }
                    return dic;
                }
            }
        }

        #endregion

        #region 修改抄送状态为已阅

        /// <summary>
        /// 修改抄送状态为已阅
        /// </summary>
        /// <param name="TaskId">流水号</param>
        /// <param name="UserId">用户Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("ChangeSendState")]
        public NewErrorModel ChangeSendState(string TaskId, string UserId)
        {
            try
            {
                using (DDContext context = new DDContext())
                {
                    Tasks task = context.Tasks.Where(t => t.TaskId.ToString() == TaskId && t.ApplyManId == UserId
                     && t.IsSend == true).OrderByDescending(u => u.Id).First();

                    task.State = 1;
                    task.ApplyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    context.Entry<Tasks>(task).State = EntityState.Modified;
                    context.SaveChanges();
                    return new NewErrorModel()
                    {
                        error = new Error(0, "修改成功！", "") { },
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

        #region 流程类别及数据读取

        /// <summary>
        /// 流程界面信息读取接口
        /// </summary>
        /// <param name="id">用户Id，用于判断权限(预留，暂时不做)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadFlowInfo")]
        public NewErrorModel LoadFlowInfo(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    FlowInfoServer flowInfoServer = new FlowInfoServer();

                    return new NewErrorModel()
                    {
                        data = flowInfoServer.GetFlowInfo(),
                        error = new Error(0, "读取成功！", "") { },
                    };
                }

                return new NewErrorModel()
                {
                    error = new Error(1, "id不能为空！", "") { },
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
        /// 流程节点信息获取接口
        /// </summary>
        /// <param name="FlowId">流程Id</param>
        /// <param name="NodeId">节点Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetNodeInfo")]
        public NewErrorModel GetNodeInfo(string FlowId, string NodeId)
        {
            try
            {
                if (FlowId != null)
                {
                    DDContext context = new DDContext();


                    if (string.IsNullOrEmpty(NodeId))
                    {
                        var NodeInfo = context.NodeInfo.Where(u => u.FlowId == FlowId);

                        return new NewErrorModel()
                        {
                            data = NodeInfo,
                            error = new Error(0, "读取成功！", "") { },
                        };
                    }
                    else
                    {
                        var NodeInfo = context.NodeInfo.Where(u => u.NodeId.ToString() == NodeId && u.FlowId == FlowId);

                        return new NewErrorModel()
                        {
                            data = NodeInfo,
                            error = new Error(0, "读取成功！", "") { },
                        };
                    }
                }
                else
                {
                    return new NewErrorModel()
                    {
                        error = new Error(1, "参数未传递！", "") { },
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
        /// 流程界面分类信息读取接口
        /// </summary>
        /// <param name="id">用户Id，用于判断权限(预留，暂时不做)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("LoadFlowSort")]
        public NewErrorModel LoadFlowSort(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    FlowInfoServer flowInfoServer = new FlowInfoServer();
                    return new NewErrorModel()
                    {
                        data = flowInfoServer.GetFlowSort(),
                        error = new Error(0, "读取成功！", "") { },
                    };
                }

                return new NewErrorModel()
                {
                    error = new Error(1, "id不能为空！", "") { },
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
        #endregion

        #region 左侧审批菜单栏状态读取

        /// <summary>
        /// 左侧审批状态(数量)数据读取
        /// </summary>
        /// <param name="ApplyManId">用户名Id</param>
        /// <returns>返回待审批的、我发起的、抄送我的数量</returns>
        /// 测试数据 /FlowInfo/GetFlowStateCounts?ApplyManId=123456
        [HttpGet]
        public string GetFlowStateCounts(string ApplyManId)
        {
            try
            {
                using (DDContext context = new DDContext())
                {
                    //待审批的
                    int iApprove = context.Tasks.Where(u => u.ApplyManId == ApplyManId && u.IsEnable == 1 && u.NodeId != 0 && u.IsSend == false && u.State == 0 && u.IsPost == false).Count();
                    //我发起的
                    int iMyPost = context.Tasks.Where(u => u.ApplyManId == ApplyManId && u.IsEnable == 1 && u.NodeId == 0 && u.IsSend == false && u.State == 1 && u.IsPost == true).Count();
                    //抄送我的
                    int iSendMy = context.Tasks.Where(u => u.ApplyManId == ApplyManId && u.IsEnable == 1 && u.NodeId != 0 && u.IsSend == true && u.State == 0 && u.IsPost == false).Count();
                    Dictionary<string, int> dic = new Dictionary<string, int>();
                    dic.Add("ApproveCount", iApprove);
                    dic.Add("MyPostCount", iMyPost);
                    dic.Add("SendMyCount", iSendMy);

                    return JsonConvert.SerializeObject(dic);
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new ErrorModel
                {
                    errorCode = 1,
                    errorMessage = ex.Message
                });
            }
        }



        /// <summary>
        /// 左侧审批状态详细数据读取
        /// </summary>
        /// <param name="Index">(Index=0:待我审批 1:我已审批 2:我发起的 3:抄送我的)</param>
        /// <param name="ApplyManId">用户名Id</param>
        /// <param name="IsSupportMobile">是否是手机端调用接口(默认 false)</param>
        /// <param name="Key">关键字模糊查询(流水号、标题、申请人、流程类型)</param>
        /// <returns> State 0 未完成 1 已完成 2 被退回</returns>
        [HttpGet]
        [Route("GetFlowStateDetail")]
        public NewErrorModel GetFlowStateDetail(int Index, string ApplyManId, bool IsSupportMobile = false, string Key = "")
        {
            try
            {
                List<int?> ListTasks = new List<int?>();
                using (DDContext context = new DDContext())
                {
                    switch (Index)
                    {
                        case 0:
                            //待审批的
                            ListTasks = context.Tasks.Where(u => u.ApplyManId == ApplyManId && u.IsEnable == 1 && u.NodeId != 0 && u.IsSend == false && u.State == 0 && u.IsPost != true && u.ApplyTime == null).OrderByDescending(u => u.TaskId).Select(u => u.TaskId).ToList();

                            return new NewErrorModel()
                            {
                                data = Quary(context, ListTasks, ApplyManId, IsSupportMobile, Key),
                                error = new Error(0, "读取成功！", "") { },
                            };
                        case 1:
                            //我已审批
                            ListTasks = context.Tasks.Where(u => u.ApplyManId == ApplyManId && u.IsEnable == 1 && u.NodeId != 0 && u.IsSend == false && u.State == 1 && u.IsPost != true && u.ApplyTime != null).OrderByDescending(u => u.TaskId).Select(u => u.TaskId).ToList();

                            return new NewErrorModel()
                            {
                                data = Quary(context, ListTasks, ApplyManId, IsSupportMobile, Key),
                                error = new Error(0, "读取成功！", "") { },
                            };
                        case 2:
                            //我发起的
                            ListTasks = context.Tasks.Where(u => u.ApplyManId == ApplyManId && u.IsEnable == 1 && u.NodeId == 0 && u.IsSend == false && u.State == 1 && u.IsPost == true && u.ApplyTime != null).OrderByDescending(u => u.TaskId).Select(u => u.TaskId).ToList();
                            return new NewErrorModel()
                            {
                                data = Quary(context, ListTasks, ApplyManId, IsSupportMobile, Key),
                                error = new Error(0, "读取成功！", "") { },
                            };
                        case 3:
                            //抄送我的
                            ListTasks = context.Tasks.Where(u => u.ApplyManId == ApplyManId && u.IsEnable == 1 && u.NodeId != 0 && u.IsSend == true && u.IsPost != true).OrderByDescending(u => u.TaskId).Select(u => u.TaskId).ToList();
                            return new NewErrorModel()
                            {
                                data = Quary(context, ListTasks, ApplyManId, IsSupportMobile, Key),
                                error = new Error(0, "读取成功！", "") { },
                            };
                        default:
                            return new NewErrorModel()
                            {
                                error = new Error(1, "参数不正确！", "") { },
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
        /// <summary>
        /// 辅助查询
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ListTasks"></param>
        /// <param name="ApplyManId"></param>
        /// <param name="IsMobile"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Quary")]
        public object Quary(DDContext context, List<int?> ListTasks,
            string ApplyManId, bool IsMobile, string Key)
        {
            FlowInfoServer flowInfoServer = new FlowInfoServer();
            List<object> listQuary = new List<object>();
            List<object> listQuaryPro = new List<object>();
            List<Tasks> ListTask = context.Tasks.ToList();
            List<Flows> ListFlows = context.Flows.ToList();
            foreach (int TaskId in ListTasks)
            {
                int StateCount = ListTask.Where(t => t.TaskId.ToString() == TaskId.ToString() && t.State == 0 && t.IsSend != true).Count();
                int? NodeId = 0;
                if (StateCount == 0)
                {
                    NodeId = ListTask.Where(t => t.TaskId.ToString() == TaskId.ToString()).Max(n => n.NodeId);
                }
                else
                {
                    if (StateCount > 1)
                    {
                        NodeId = ListTask.Where(t => t.TaskId.ToString() == TaskId.ToString() && t.State == 0 && t.IsSend != true).OrderBy(u => u.NodeId).Select(u => u.NodeId).ToList().First();
                    }
                    else
                    {
                        NodeId = ListTask.Where(t => t.TaskId.ToString() == TaskId.ToString() && t.State == 0 && t.IsSend != true).Select(u => u.NodeId).ToList().First();
                    }
                }
                var query = from t in ListTask
                            join f in ListFlows
                            on t.FlowId.ToString() equals f.FlowId.ToString()
                            where t.NodeId == 0 && t.TaskId == TaskId
                            && (IsMobile == true ? f.IsSupportMobile == true : 1 == 1)
                            && ((Key != "" ? f.FlowName.Contains(Key) : 1 == 1) ||
                                (Key != "" ? t.TaskId.ToString().Contains(Key) : 1 == 1) ||
                                 (Key != "" ? t.Title.ToString().Contains(Key) : 1 == 1) ||
                                (Key != "" ? t.ApplyMan.Contains(Key) : 1 == 1)
                            )
                            select new
                            {
                                Id = t.Id + 1,
                                TaskId = t.TaskId,
                                NodeId = NodeId,
                                FlowId = t.FlowId,
                                FlowName = f.FlowName,
                                ApplyMan = t.ApplyMan,
                                ApplyManId = t.ApplyManId,
                                ApplyTime = t.ApplyTime,
                                Title = t.Title,
                                State = GetTasksState(t.TaskId.ToString(), ListTask),
                                IsBack = t.IsBacked,
                                IsSupportMobile = f.IsSupportMobile
                            };

                if (query.Count() > 0)
                {
                    listQuary.Add(query);
                }
            }

            string strJson = JsonConvert.SerializeObject(listQuary);
            List<List<TaskFlowModel>> TaskFlowModelListList = JsonConvert.DeserializeObject<List<List<TaskFlowModel>>>(strJson);
            List<TaskFlowModel> TaskFlowModelList = new List<TaskFlowModel>();
            List<TaskFlowModel> TaskFlowModelListQuery = new List<TaskFlowModel>();
            List<List<TaskFlowModel>> TaskFlowModelListListPro = new List<List<TaskFlowModel>>();
            foreach (var item in TaskFlowModelListList)
            {
                TaskFlowModelList.Add(item[0]);
            }

            foreach (var item in TaskFlowModelList)
            {
                if (!TaskFlowModelListQuery.Contains(item))
                {
                    List<TaskFlowModel> taskFlowModels = TaskFlowModelListQuery.Where(t => t.TaskId == item.TaskId).ToList();
                    if (taskFlowModels.Count == 0)
                    {
                        TaskFlowModelListQuery.Add(item);
                    }
                }
            }
            return TaskFlowModelListQuery;
        }

        /// <summary>
        /// 获取流程状态
        /// </summary>
        /// <param name="TaskId"></param>
        /// <param name="ListTask"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetTasksState")]
        public string GetTasksState(string TaskId, List<Tasks> ListTask)
        {
            List<Tasks> tasksListBack = ListTask.Where(t => t.TaskId.ToString() == TaskId && t.IsBacked == true).ToList();
            if (tasksListBack.Count > 0)
            {
                foreach (Tasks task in tasksListBack)
                {
                    if (task.NodeId == 0)
                    {
                        return "已撤回";
                    }
                    else
                    {
                        return "被退回";
                    }
                }
            }
            List<Tasks> tasksListFinished = ListTask.Where(t => t.TaskId.ToString() == TaskId && t.State == 0 && t.IsSend != true).ToList();
            if (tasksListFinished.Count > 0)
            {
                return "未完成";
            }
            else
            {
                return "已完成";
            }
        }


        #endregion

        #region 审批意见数据读取

        /// <summary>
        /// 审批意见数据读取
        /// </summary>
        /// <param name="TaskId">流水号</param>
        /// <param name="FlowId">流程Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetSign")]
        public NewErrorModel GetSign(string TaskId, string FlowId)
        {
            try
            {
                if (string.IsNullOrEmpty(TaskId))  //尚未发起流程
                {
                    using (DDContext context = new DDContext())
                    {
                        List<NodeInfo> NodeInfoList = context.NodeInfo.Where(n => n.FlowId == FlowId).ToList();

                        var Quary = from n in NodeInfoList
                                    orderby n.NodeId
                                    select new
                                    {
                                        NodeId = n.NodeId,
                                        NodeName = n.NodeName,
                                        IsBack = false,
                                        ApplyMan = n.NodePeople,
                                        ApplyTime = "",
                                        Remark = "",
                                        IsSend = "",
                                        IsNeedChose = n.IsNeedChose,
                                        ChoseNodeId = n.ChoseNodeId
                                    };
                        return new NewErrorModel()
                        {
                            data = Quary,
                            error = new Error(0, "读取成功！", "") { },
                        };

                    }
                }
                else
                {
                    using (DDContext context = new DDContext())
                    {
                        List<NodeInfo> NodeInfoList = context.NodeInfo.Where(u => u.FlowId == FlowId).ToList();
                        List<Tasks> TaskList = context.Tasks.Where(u => u.TaskId.ToString() == TaskId && u.IsBacked != false).ToList();
                        var Quary = from n in NodeInfoList
                                    join t in TaskList
                                    on n.NodeId equals t.NodeId
                                    into temp
                                    from tt in temp.DefaultIfEmpty()
                                    orderby n.NodeId
                                    select new
                                    {
                                        NodeId = n.NodeId,
                                        NodeName = n.NodeName,
                                        IsBack = tt == null ? false : tt.IsBacked,
                                        ApplyMan = tt == null ? n.NodePeople : tt.ApplyMan,
                                        ApplyTime = tt == null ? "" : tt.ApplyTime,
                                        Remark = tt == null ? "" : tt.Remark,
                                        IsSend = tt == null ? n.IsSend : tt.IsSend,
                                        ApplyManId = tt == null ? "" : tt.ApplyManId
                                    };
                        Quary = Quary.OrderBy(q => q.NodeId).ThenByDescending(h => h.ApplyTime);
                        return new NewErrorModel()
                        {
                            data = Quary,
                            error = new Error(0, "读取成功！", "") { },
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

        #endregion

        #region 流程节点人员配置

        /// <summary>
        /// 流程节点人员配置
        /// </summary>
        /// <param name="NodeInfoList"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("UpdateNodeInfo")]
        public NewErrorModel UpdateNodeInfo([FromBody]List<NodeInfo> NodeInfoList)
        {
            try
            {
                using (DDContext context = new DDContext())
                {
                    foreach (NodeInfo nodeInfo in NodeInfoList)
                    {
                        context.Entry<NodeInfo>(nodeInfo).State = EntityState.Modified;
                    }
                    context.SaveChanges();
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

        #endregion

        #region 审批页面通用数据读取

        /// <summary>
        /// 审批页面通用数据读取
        /// </summary>
        /// <param name="TaskId">流水号</param>
        /// <param name="ApplyManId">用户Id</param>
        /// <returns></returns>
        /// 测试数据 /FlowInfo/GetApproveInfo?TaskId=7
        public string GetApproveInfo(string TaskId, string ApplyManId)
        {
            try
            {
                if (string.IsNullOrEmpty(TaskId))
                {
                    return JsonConvert.SerializeObject(new ErrorModel
                    {
                        errorCode = 1,
                        errorMessage = "请传递参数"
                    });
                }
                else
                {
                    using (DDContext context = new DDContext())
                    {
                        Tasks task = context.Tasks.Where(u => u.TaskId.ToString() == TaskId && u.ApplyManId == ApplyManId && u.IsEnable == 1).OrderByDescending(t => t.Id).First();
                        Tasks taskOld = context.Tasks.Where(u => u.TaskId.ToString() == TaskId && u.NodeId == 0).First();
                        taskOld.Id = task.Id;
                        taskOld.NodeId = task.NodeId;
                        return JsonConvert.SerializeObject(taskOld);
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new ErrorModel
                {
                    errorCode = 2,
                    errorMessage = ex.Message
                });
            }
        }
        #endregion

        #region 系统已配置人员信息读取

        /// <summary>
        /// 系统已配置人员信息读取
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GetUserInfo")]
        public NewErrorModel GetUserInfo()
        {
            try
            {
                DDContext context = new DDContext();
                var NodeInfoList = context.NodeInfo;
                var Quary = from n in NodeInfoList
                            where n.PeopleId != null && !n.PeopleId.Contains(",")
                            select new
                            {
                                n.PeopleId,
                                n.NodePeople
                            };
                var QuaryPro = from n in Quary
                               group n by new { PeopleId = n.PeopleId, NodePeople = n.NodePeople }
                               into g
                               select new { g.Key.PeopleId, g.Key.NodePeople };

                return new NewErrorModel()
                {
                    data = QuaryPro,
                    error = new Error(0, "读取成功！", "") { },
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

        #endregion

        #region 钉钉SDK再封装接口

        /// <summary>
        /// 发送OA消息
        /// </summary>
        [HttpGet]
        [Route("TestSentOaMsg")]
        public string TestSentOaMsg()
        {
            TopSDKTest top = new TopSDKTest();
            OATextModel oaTextModel = new OATextModel();
            oaTextModel.message_url = "https://www.baidu.com/";
            oaTextModel.head = new head
            {
                bgcolor = "FFBBBBBB",
                text = "头部标题111"
            };
            oaTextModel.body = new body
            {
                form = new form[] {
                    new form{ key="姓名",value="11张三"},
                    new form{ key="爱好",value="打球"},
                },
                rich = new rich
                {
                    num = "15.6",
                    unit = "元"
                },
                //title = "正文标题",
                content = "111一大段文字",
                image = "@lADOADmaWMzazQKA",
                file_count = "3",
                author = "李四"
            };
            return top.SendOaMessage("083452125733424957", oaTextModel);
        }

        /// <summary>
        /// 推送OA消息
        /// </summary>
        /// <param name="FlowId"></param>
        /// <param name="ApplyManId"></param>
        /// <param name="TaskId"></param>
        /// <param name="ApplyMan"></param>
        /// <param name="Remark"></param>
        /// <param name="dDContext"></param>
        /// <param name="IsBack"></param>
        /// <param name="IsSend"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("SendOaMsgNew")]
        public async Task<object> SendOaMsgNew(int? FlowId, string ApplyManId, string TaskId, string ApplyMan,
            string Remark, DDContext dDContext, bool IsBack = false, bool IsSend = false)
        {
            DingTalkServersController dingTalkServersController = new DingTalkServersController();
            //推送OA消息
            if (dDContext.Flows.Where(f => f.FlowId.ToString() == FlowId.ToString()).First().IsSupportMobile == true)
            {
                if (IsBack)
                {
                    return await dingTalkServersController.sendOaMessage(ApplyManId,
                   string.Format("您的一条被退回的流程(流水号:{0})，详情请及点击进入华数机器人信息管理系统进行查阅。(Ps:如果点击没有反应，请尝试升级手机钉钉版本)", TaskId),
                   ApplyMan, "eapp://page/approve/approve?index=2");
                }
                else
                {
                    if (IsSend)
                    {
                        return await dingTalkServersController.sendOaMessage(ApplyManId,
                  string.Format("您有一条抄送的流程(流水号:{0})，请及点击进入华数机器人信息管理系统进行查阅。(Ps:如果点击没有反应，请尝试升级手机钉钉版本)", TaskId),
                  ApplyMan, "eapp://page/approve/approve?index=3");
                    }
                    else
                    {
                        return await dingTalkServersController.sendOaMessage(ApplyManId,
                   string.Format("您有一条待审批的流程(流水号:{0})，请及点击进入华数机器人信息管理系统进行审批。(Ps:如果点击没有反应，请尝试升级手机钉钉版本)", TaskId),
                   ApplyMan, "eapp://page/approve/approve?index=0");
                    }
                }
            }
            else
            {
                if (IsBack)
                {
                    SentCommonMsg(ApplyManId, string.Format("您有一条被退回的流程(流水号:{0})，请进入华数机器人信息管理系统进行查阅。", TaskId), ApplyMan, Remark, null);
                }
                else
                {
                    if (IsSend)
                    {
                        SentCommonMsg(ApplyManId, string.Format("您有一条抄送的流程(流水号:{0})，请及进入华数机器人信息管理系统进行查阅。", TaskId), ApplyMan, Remark, null);
                    }
                    else
                    {
                        SentCommonMsg(ApplyManId, string.Format("您有一条待审批的流程(流水号:{0})，请及进入华数机器人信息管理系统进行审批。", TaskId), ApplyMan, Remark, null);
                    }
                }
                
                return dingTalkServersController.sendOaMessage("测试",
                       string.Format("您有一条待审批的流程(流水号:{0})，请及进入研究院信息管理系统进行审批。", TaskId),
                       ApplyMan, "eapp://page/approve/approve");
            }
        }

        /// <summary>
        /// 发送普通消息
        /// </summary>
        /// <param name="SendPeoPleId"></param>
        /// <param name="Title"></param>
        /// <param name="ApplyMan"></param>
        /// <param name="Content"></param>
        /// <param name="Url"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("SentCommonMsg")]
        public string SentCommonMsg(string SendPeoPleId, string Title, string ApplyMan,
            string Content, string Url)
        {
            TopSDKTest top = new TopSDKTest();
            OATextModel oaTextModel = new OATextModel();
            oaTextModel.head = new head
            {
                bgcolor = "FFBBBBBB",
                text = "您有一条待审批的流程，请登入OA系统审批"
            };
            oaTextModel.body = new body
            {
                form = new form[] {
                    new form{ key="申请人：",value=ApplyMan},
                    new form{ key="申请时间：",value=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                },
                //rich = new rich
                //{
                //    num = "15.6",
                //    unit = "元"
                //},
                title = Title,//"您有一条待审批的流程，请登入OA系统审批",
                content = Content//"我要请假~~~~123456",
                                 //image = "@lADOADmaWMzazQKA",
                                 //file_count = "3",
            };
            oaTextModel.message_url = Url;
            return top.SendOaMessage(SendPeoPleId, oaTextModel);
        }


        #endregion
    }
}
