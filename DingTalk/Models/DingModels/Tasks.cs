namespace DingTalk.Models.DingModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Tasks
    {
        [Column(TypeName = "numeric")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public decimal Id { get; set; }
        /// <summary>
        /// ��ˮ��
        /// </summary>
        public int? TaskId { get; set; }
        /// <summary>
        /// �ύ��
        /// </summary>
        [StringLength(50)]
        public string ApplyMan { get; set; }
        /// <summary>
        /// �ύ��Id
        /// </summary>
        [StringLength(500)]
        public string ApplyManId { get; set; }
        /// <summary>
        /// ��������
        /// </summary>
        [StringLength(200)]
        public string Dept { get; set; }
        /// <summary>
        /// �ύʱ��
        /// </summary>
        [StringLength(30)]
        public string ApplyTime { get; set; }
        /// <summary>
        /// �Ƿ�����
        /// </summary>
        public int? IsEnable { get; set; }
        /// <summary>
        /// ����Id
        /// </summary>
        public int? FlowId { get; set; }
        /// <summary>
        /// �ڵ�Id
        /// </summary>
        public int? NodeId { get; set; }
        /// <summary>
        /// ��ע
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// �Ƿ���
        /// </summary>
        public bool? IsSend { get; set; }
        /// <summary>
        /// ��ǰ״̬
        /// </summary>
        public int? State { get; set; }
        /// <summary>
        /// ͼƬ·��
        /// </summary>
        public string ImageUrl { get; set; }
        /// <summary>
        /// �ļ�·��
        /// </summary>
        public string FileUrl { get; set; }
        /// <summary>
        /// ����
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// ��ĿId
        /// </summary>
        [StringLength(500)]
        public string ProjectId { get; set; }
        /// <summary>
        /// �Ƿ��Ƿ���
        /// </summary>
        public bool? IsPost { get; set; }
        /// <summary>
        /// ԭͼƬ·��
        /// </summary>
        public string OldImageUrl { get; set; }
        /// <summary>
        /// ԭ�ļ�·��
        /// </summary>
        public string OldFileUrl { get; set; }
        /// <summary>
        /// �Ƿ��˻�
        /// </summary>
        public bool? IsBacked { get; set; }
        /// <summary>
        /// �����ļ�Id
        /// </summary>
        public string MediaId { get; set; }
        /// <summary>
        /// PDF�ļ�·��
        /// </summary>
        public string FilePDFUrl { get; set; }
        /// <summary>
        /// ԭPDF�ļ�·��
        /// </summary>
        public string OldFilePDFUrl { get; set; }
        /// <summary>
        /// PDF����Id
        /// </summary>
        public string MediaIdPDF { get; set; }
        /// <summary>
        /// PDF״̬
        /// </summary>
        [StringLength(500)]
        public string PdfState { get; set; }
        /// <summary>
        /// ��Ŀ����
        /// </summary>
        [StringLength(200)]
        public string ProjectName { get; set; }
        /// <summary>
        /// ����
        /// </summary>
        [StringLength(100)]
        public string counts { get; set; }

        /// <summary>
        /// �ڵ�����
        /// </summary>
        [StringLength(200)]
        public string NodeName { get; set; }


        /// <summary>
        /// ��Ŀ��������(�����ӹ�����е�ӹ�����е�ɹ�������)
        /// </summary>
        [StringLength(200)]
        public string ProjectType { get; set; }
    }
}
