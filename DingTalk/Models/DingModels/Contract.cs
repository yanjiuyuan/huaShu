namespace DingTalk.Models.DingModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Contract")]
    public partial class Contract
    {
        [Column(TypeName = "numeric")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public decimal Id { get; set; }
        /// <summary>
        /// 流水号(预留)
        /// </summary>
        [StringLength(300)]
        public string TaskId { get; set; }
        /// <summary>
        /// 合同编号
        /// </summary>
        [StringLength(300)]
        public string ContractNo { get; set; }
        /// <summary>
        /// 合同名称
        /// </summary>
        [StringLength(200)]
        public string ContractName { get; set; }

        /// <summary>
        /// 合同签订单位
        /// </summary>
        [StringLength(300)]
        public string SignUnit { get; set; }
        /// <summary>
        /// 销售经理
        /// </summary>
        [StringLength(300)]
        public string SalesManager { get; set; }
        /// <summary>
        /// 合同类型
        /// </summary>
        [StringLength(300)]
        public string ContractType { get; set; }
        /// <summary>
        /// 合同路径(预留)
        /// </summary>
        [StringLength(500)]
        public string Path { get; set; }
        /// <summary>
        /// 合同子文件(预留)
        /// </summary>
        [StringLength(500)]
        public string FilePath { get; set; }

        /// <summary>
        /// 销售经理Id
        /// </summary>
        [StringLength(200)]
        public string SalesManagerId { get; set; }

        /// <summary>
        /// 操作员Id
        /// </summary>
        [NotMapped]
        public string ApplyManId { get; set; }
    }
}
