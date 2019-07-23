namespace DingTalk.Models.DingModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ProjectInfo")]
    public partial class ProjectInfo
    {
        [Column(TypeName = "numeric")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public decimal Id { get; set; }

        /// <summary>
        /// ��Ŀ����
        /// </summary>
        [StringLength(500)]
        public string ProjectName { get; set; }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        [StringLength(100)]
        public string CreateTime { get; set; }

        /// <summary>
        /// �Ƿ�����
        /// </summary>
        public bool? IsEnable { get; set; }

        /// <summary>
        /// ��Ŀ״̬
        /// </summary>
        [StringLength(100)]
        public string ProjectState { get; set; }

        /// <summary>
        /// ��������
        /// </summary>
        [StringLength(200)]
        public string DeptName { get; set; }

        /// <summary>
        /// ������
        /// </summary>
        [StringLength(200)]
        public string ApplyMan { get; set; }

        /// <summary>
        /// ������Id
        /// </summary>
        [StringLength(300)]
        public string ApplyManId { get; set; }

        /// <summary>
        /// ��Ŀ��ʼʱ��
        /// </summary>
        [StringLength(200)]
        public string StartTime { get; set; }

        /// <summary>
        /// ��Ŀ����ʱ��
        /// </summary>
        [StringLength(200)]
        public string EndTime { get; set; }

        /// <summary>
        /// ��Ŀ���
        /// </summary>
        [StringLength(200)]
        public string ProjectId { get; set; }

        /// <summary>
        /// ��Ŀ·��
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// ��Ŀ������
        /// </summary>
        [StringLength(200)]
        public string ResponsibleMan { get; set; }

        /// <summary>
        /// ��Ŀ������Id
        /// </summary>
        [StringLength(200)]
        public string ResponsibleManId { get; set; }

        /// <summary>
        /// ������λ
        /// </summary>
        [StringLength(200)]
        public string CompanyName { get; set; }

        /// <summary>
        /// ��Ŀ����
        /// </summary>
        [StringLength(200)]
        public string ProjectType { get; set; }

        /// <summary>
        /// ��ĿС��(����)
        /// </summary>
        [StringLength(200)]
        public string ProjectSmallType { get; set; }

        /// <summary>
        /// С���Ա
        /// </summary>
        public string TeamMembers { get; set; }

        /// <summary>
        /// С���ԱId
        /// </summary>
        public string TeamMembersId { get; set; }
    }
}
