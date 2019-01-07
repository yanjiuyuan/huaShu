namespace DingTalk.Models.DingModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PurchaseTable")]
    public partial class PurchaseTable
    {
        [Column(TypeName = "numeric")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public decimal Id { get; set; }
        /// <summary>
        /// 流水号
        /// </summary>
        [StringLength(300)]
        public string TaskId { get; set; }
        /// <summary>
        /// 物料编码
        /// </summary>
        [StringLength(500)]
        public string CodeNo { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        [StringLength(300)]
        public string Name { get; set; }
        /// <summary>
        /// 规格
        /// </summary>
        [StringLength(300)]
        public string Standard { get; set; }
        /// <summary>
        /// 单位
        /// </summary>
        [StringLength(500)]
        public string Unit { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        [StringLength(500)]
        public string Count { get; set; }
        /// <summary>
        /// 价格
        /// </summary>
        [StringLength(500)]
        public string Price { get; set; }
        /// <summary>
        /// 目的
        /// </summary>
        [StringLength(500)]
        public string Purpose { get; set; }
        /// <summary>
        /// 需用日期
        /// </summary>
        [StringLength(500)]
        public string UrgentDate { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string Mark { get; set; }

        /// <summary>
        /// 采购员
        /// </summary>
        [StringLength(100)]
        public string PurchaseMan { get; set; }
        /// <summary>
        /// 采购员Id
        /// </summary>
        [StringLength(100)]
        public string PurchaseManId { get; set; }
        /// <summary>
        /// 采购类型(零部件采购、成品采购、客户服务配件采购)
        /// </summary>
        [StringLength(50)]
        public string purchaseType { get; set; }
        /// <summary>
        /// 送货地点
        /// </summary>
        [StringLength(100)]
        public string SendPosition { get; set; }

    }
}
