﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using K12.Data;
using FISCA.Presentation;
using K12.Presentation;

namespace K12StudentPhoto
{
    public partial class PhotosBatchExportForm : FISCA.Presentation.Controls.BaseForm
    {
        private PhotoBatchFileManager PhotoBatchFileManager1;
        private List<StudentRecord> SelStudenrList;
        private StudPhotoEntity.PhotoKind PhotoKind;
        private StudPhotoEntity.PhotoNameRule PhotoNameRule;
        private List<string> _ExportFolderName;

        public PhotosBatchExportForm()
        {
            InitializeComponent();
            cbxEnroll.Checked = true;
            cbxByStudentNum.Checked = true;
            _ExportFolderName = new List<string>();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            PhotoBatchFileManager1 = new PhotoBatchFileManager();
            txtFilePath.Text = PhotoBatchFileManager1.GetFilefoldrBrowserDialog();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (PhotoBatchFileManager1 == null)
                PhotoBatchFileManager1 = new PhotoBatchFileManager();

            if (string.IsNullOrEmpty(txtFilePath.Text))
            {
                MessageBox.Show("請輸入匯出路徑");
                return;
            }

            if (PhotoBatchFileManager1.checkHasFolder(txtFilePath.Text) == false)
            {
                MessageBox.Show("匯出目錄不存在");
                return;
            }

            if (cbxEnroll.Checked)
                PhotoKind = StudPhotoEntity.PhotoKind.入學;

            if (cbxGraduate.Checked)
                PhotoKind = StudPhotoEntity.PhotoKind.畢業;

            if (cbxByStudentNum.Checked)
                PhotoNameRule = StudPhotoEntity.PhotoNameRule.學號;

            if (cbxByStudentIDNumber.Checked)
                PhotoNameRule = StudPhotoEntity.PhotoNameRule.身分證號;

            if (cbxByClassNameSeatNo.Checked)
                PhotoNameRule = StudPhotoEntity.PhotoNameRule.班級座號;


            SelStudenrList = new List<StudentRecord>();
            SelStudenrList = Student.SelectByIDs(NLDPanels.Student.SelectedSource);
            List<StudPhotoEntity> StudPhotoEntityList = new List<StudPhotoEntity>();

            _ExportFolderName.Clear();
            foreach (StudentRecord studRec in SelStudenrList)
            {
                StudPhotoEntity spe = new StudPhotoEntity();
                spe.StudentNumber = studRec.StudentNumber;
                spe.StudentID = studRec.ID;
                spe.SeatNo = K12.Data.Int.GetString(studRec.SeatNo);
                spe.StudentName = studRec.Name;
                spe.StudentIDNumber = studRec.IDNumber;
                if (studRec.Class != null)
                    spe.ClassName = studRec.Class.Name;
                spe._PhotoKind = PhotoKind;
                spe._PhotoNameRule = PhotoNameRule;

                if (spe._PhotoNameRule == StudPhotoEntity.PhotoNameRule.學號)
                {
                    spe.SaveFilePathAndName = txtFilePath.Text + "\\" + spe.StudentNumber + ".jpg";
                    //added by Cloud
                    ClassifyByClassName(spe, spe.StudentNumber);
                }
                    

                if (spe._PhotoNameRule == StudPhotoEntity.PhotoNameRule.身分證號)
                {
                    spe.SaveFilePathAndName = txtFilePath.Text + "\\" + spe.StudentIDNumber + ".jpg";
                    //added by Cloud
                    ClassifyByClassName(spe, spe.StudentIDNumber);
                }
                    

                if (spe._PhotoNameRule == StudPhotoEntity.PhotoNameRule.班級座號)
                {
                    string exportFolderName = txtFilePath.Text + "\\" + spe.ClassName;
                    spe.SaveFilePathAndName = exportFolderName + "\\" + spe.SeatNo + ".jpg";
                    if (!_ExportFolderName.Contains(exportFolderName))
                        _ExportFolderName.Add(exportFolderName);
                }

                StudPhotoEntityList.Add(spe);
            }

            StudPhotoEntityList = Transfer.GetStudentPhotoBitmap(StudPhotoEntityList);

            Dictionary<string, Bitmap> saveFreshmanFiles = new Dictionary<string, Bitmap>();
            Dictionary<string, Bitmap> saveGraduateFiles = new Dictionary<string, Bitmap>();
            // 入學
            if (cbxEnroll.Checked)
            {
                foreach (StudPhotoEntity spe in StudPhotoEntityList)
                {
                    if (!saveFreshmanFiles.ContainsKey(spe.SaveFilePathAndName))
                    {
                        if (spe.FreshmanPhotoBitmap == null)
                            spe.ErrorMessage += "沒有照片, 命名方式:" + spe._PhotoNameRule.ToString() + "," + spe.SaveFilePathAndName;
                        else
                            saveFreshmanFiles.Add(spe.SaveFilePathAndName, spe.FreshmanPhotoBitmap);
                    }
                    else
                        spe.ErrorMessage += "檔名有重複無法匯出, 命名方式:" + spe._PhotoNameRule.ToString() + "," + spe.SaveFilePathAndName;
                }
            }

            // 畢業
            if (cbxGraduate.Checked)
            {
                foreach (StudPhotoEntity spe in StudPhotoEntityList)
                {
                    if (!saveGraduateFiles.ContainsKey(spe.SaveFilePathAndName))
                    {
                        if (spe.GraduatePhotoBitmap == null)
                            spe.ErrorMessage += " 沒有照片, 命名方式:" + spe._PhotoNameRule.ToString() + "," + spe.SaveFilePathAndName;
                        else
                            saveGraduateFiles.Add(spe.SaveFilePathAndName, spe.GraduatePhotoBitmap);
                    }
                    else
                        spe.ErrorMessage += " 檔名有重複無法匯出, 命名方式:" + spe._PhotoNameRule.ToString() + "," + spe.SaveFilePathAndName;
                }
            }

            // 收集錯誤訊息
            List<StudPhotoEntity> ErrorData = new List<StudPhotoEntity>();


            foreach (StudPhotoEntity spe in StudPhotoEntityList)
            {
                if (!string.IsNullOrEmpty(spe.ErrorMessage))
                    ErrorData.Add(spe);
            }

            // 當選擇班級時或以班級名稱分類時建立資料夾 modified by Cloud
            if (cbxByClassNameSeatNo.Checked || checkBoxX1.Checked)
            {
                foreach (string str in _ExportFolderName)
                    PhotoBatchFileManager1.CreateFolder(str);
            }

            if (saveFreshmanFiles.Count > 0)
                PhotoBatchFileManager1.SaveFiles(saveFreshmanFiles);
            if (saveGraduateFiles.Count > 0)
                PhotoBatchFileManager1.SaveFiles(saveGraduateFiles);

            // 匯出完後畫面顯示
            PhotoCompleteMessage pcm = new PhotoCompleteMessage();
            string msg = "匯出檔案數:" + StudPhotoEntityList.Count + ", 匯出成功數:" + (StudPhotoEntityList.Count - ErrorData.Count) + " ,匯出失敗數:" + ErrorData.Count;

            //PermRecLogProcess prlp = new PermRecLogProcess();
            //prlp.SaveLog("學生,批次匯出照片", "匯出", "批次匯出學生照片," + msg);

            pcm.SetFormText("匯出照片訊息");
            pcm.SetMessageText(msg);
            if (ErrorData.Count > 0)
            {
                pcm.SetStudPhotoEntity(ErrorData);
                pcm.SetErrorMsgBottonEnable(true);
            }
            else
                pcm.SetErrorMsgBottonEnable(false);
            pcm.ShowDialog();
            //MessageBox.Show("存檔完成");
        }

        //以班級名稱分類
        private void ClassifyByClassName(StudPhotoEntity spe, String fileName)
        {
            if(checkBoxX1.Checked)
            {
                string exportFolderName = txtFilePath.Text + "\\" + spe.ClassName;
                spe.SaveFilePathAndName = exportFolderName + "\\" + fileName + ".jpg";

                if (!_ExportFolderName.Contains(exportFolderName))
                    _ExportFolderName.Add(exportFolderName);
            }
        }

        //命名方式若=班級座號,就關閉以班級分類的checkBoxX1選項 by Cloud
        private void cbxByClassNameSeatNo_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxByClassNameSeatNo.Checked)
            {
                checkBoxX1.Enabled = false;
            }
            else
            {
                checkBoxX1.Enabled = true;
            }
        }

        //若有勾選以班級分類就不會有班級座號的命名方式 by Cloud
        private void checkBoxX1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBoxX1.Checked)
            {
                cbxByClassNameSeatNo.Enabled = false;
            }
            else
            {
                cbxByClassNameSeatNo.Enabled = true;
            }
        }
    }
}
