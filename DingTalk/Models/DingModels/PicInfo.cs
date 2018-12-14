namespace DingTalk.Models.DingModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PicInfo")]
    public partial class PicInfo
    {
        [Column(TypeName = "numeric")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public decimal Id { get; set; }
        /// <summary>
        /// ����
        /// </summary>
        [StringLength(50)]
        public string Type { get; set; }
        /// <summary>
        /// ͼƬ·��
        /// </summary>
        [StringLength(300)]
        public string Path { get; set; }
        /// <summary>
        /// ����(Ԥ��)
        /// </summary>
        [StringLength(500)]
        public string Content { get; set; }
        /// <summary>
        /// ������
        /// </summary>
        [StringLength(200)]
        public string CreateMan { get; set; }
        /// <summary>
        /// ������Id
        /// </summary>
        [StringLength(200)]
        public string CreateManId { get; set; }
        /// <summary>
        /// ����ʱ��
        /// </summary>
        [StringLength(200)]
        public string CreateTime { get; set; }   
    }
}
