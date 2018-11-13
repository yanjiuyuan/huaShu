using DingTalk.EF;
using DingTalk.Models;
using DingTalk.Models.DingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DingTalk.Controllers
{
    /// <summary>
    /// 合同管理
    /// </summary>
    [RoutePrefix("ContractManager")]
    public class ContractManagerController : ApiController
    {
        /// <summary>
        /// 新增合同
        /// </summary>
        /// <param name="contract"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Add")]
        public object Add(Contract contract)
        {
            try
            {
                EFHelper<Contract> eFHelper = new EFHelper<Contract>();
                if (eFHelper.Add(contract) == 1)
                {
                    return new NewErrorModel()
                    {
                        error = new Error(0, "保存成功！", "") { },
                    };
                }
                else
                {
                    return new NewErrorModel()
                    {
                        error = new Error(1, "保存失败！", "") { },
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
        /// 修改合同
        /// </summary>
        /// <param name="contract"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Modify")]
        public object Modify(Contract contract)
        {
            try
            {
                EFHelper<Contract> eFHelper = new EFHelper<Contract>();
                eFHelper.Modify(contract);
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

        /// <summary>
        /// 根据Id删除合同
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Del")]
        public object Del(int Id)
        {
            try
            {
                EFHelper<Contract> eFHelper = new EFHelper<Contract>();                Expression<Func<Contract, bool>> expression = c => c.Id == Id;                eFHelper.DelBy(expression);
                return new NewErrorModel()
                {
                    error = new Error(0, "删除成功！", "") { },
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
        /// 合同查询与分页
        /// </summary>
        /// <param name="keyword">关键字</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页容量</param>
        /// <returns></returns>
        [HttpGet]
        [Route("Quary")]
        public object Quary(string keyword, int pageIndex, int pageSize)
        {
            try
            {
                EFHelper<Contract> eFHelper = new EFHelper<Contract>();
                System.Linq.Expressions.Expression<Func<Contract, bool>> expression = null;
                expression = n => n.ContractNo.Contains(keyword) || n.ContractType.Contains(keyword) || n.SalesManager.Contains(keyword) || n.SignUnit.Contains(keyword);
                List<Contract> contractListAll = eFHelper.GetListBy(expression);
                List<Contract> contract = eFHelper.GetPagedList(pageIndex, pageSize,
                     expression, n => n.Id);
                return new NewErrorModel()
                {
                    count = contractListAll.Count,
                    data = contract,
                    error = new Error(0, "查询成功！", "") { },
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
    }
}
