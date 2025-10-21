using System;

namespace SaveCodeClassfication.Models
{
    /// <summary>
    /// ��ū ���� ��
    /// </summary>
    public class TokenInfo
    {
        /// <summary>
        /// ���� ��ū
        /// </summary>
        public string Auth_tokens { get; set; } = string.Empty;

        /// <summary>
        /// ��ȿ ��¥
        /// </summary>
        public DateTime Effective_Date { get; set; }

        /// <summary>
        /// ��� ���� (Y/N)
        /// </summary>
        public string Use_Yn { get; set; } = "N";

        /// <summary>
        /// ���� ��¥ (DB�� �÷��� ������ ���� ��¥�� ����)
        /// </summary>
        public DateTime Create_DTM { get; set; } = DateTime.Now;

        /// <summary>
        /// ��� ���� (Boolean)
        /// </summary>
        public bool IsUsed => Use_Yn?.ToUpper() == "Y";

        /// <summary>
        /// ��ȿ�� ����
        /// </summary>
        public bool IsValid => DateTime.Now.Date <= Effective_Date.Date;

        /// <summary>
        /// ���� �ؽ�Ʈ
        /// </summary>
        public string StatusText
        {
            get
            {
                if (IsUsed) return "����";
                if (!IsValid) return "�����";
                return "��밡��";
            }
        }
    }
}