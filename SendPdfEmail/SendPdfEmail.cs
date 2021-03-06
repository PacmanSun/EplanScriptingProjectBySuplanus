//===================================================
// LUC S.
// SCHL  17-11-2016
// Send PDF by email
//===================================================
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
//===================================================
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Base;
using Eplan.EplApi.Scripting;
//===================================================


public class PDFMail
{
    string sDirPDF = @"C:\TEMP\";  // Export Path (Temp) - enter here the Export path where the PDF should be Exported. If not required just enter a local temporary folder.
	string sFileExport;
    string sProjectName;
    string sProject;

    [Start]

//################################################################################################
// PDF Export
    public void Export_PDF()
    {

	try
	    {
				// MessageBox.Show(sDirPDF);
				// Path of the selected Eplan project		
                sProject = Get_Project();
                if (sProject == "")
                    return;
				// Name of the Eplan project
                sProjectName = Get_Name(sProject);
                // PDF - Export file name / path
                sFileExport = sDirPDF + sProjectName + ".pdf";
                //MessageBox.Show("Pfad : " + sFileExport);   

                Eplan.EplApi.ApplicationFramework.ActionCallingContext aPDF = new Eplan.EplApi.ApplicationFramework.ActionCallingContext();
                aPDF.AddParameter("TYPE", "PDFPROJECTSCHEME");
                aPDF.AddParameter("PROJECTNAME", sProject);
                aPDF.AddParameter("EXPORTFILE", sFileExport);
                //aPDF.AddParameter("EXPORTSCHEME", "Standard");  // Optional - enter here your EPLAN Export Scheme instead of "Standard"
                aPDF.AddParameter("BLACKWHITE", "1"); // PDF Print as Black & White

                Eplan.EplApi.ApplicationFramework.CommandLineInterpreter aEx = new Eplan.EplApi.ApplicationFramework.CommandLineInterpreter();
                bool sRet = aEx.Execute("export", aPDF);
                
                if (!sRet)
                    {
                    System.Windows.Forms.MessageBox.Show("ERROR : PDF EXPORT");
                    }
                string SUBJECT = "Eplan Schematic : " + sProjectName;
                string BODY = "Original file path :  " + sFileExport;
                string ATTACH = sFileExport;   
               
                MAPI mapi = new MAPI();

				// Definition email adresse  / Optional

                    //string ADDRESS = "YourEmail@....";
                    //mapi.AddRecipientTo(ADDRESS);
                    //string ADDRESS2 = "YourEmail@....";
                    //mapi.AddRecipientTo(ADDRESS2);

                if (File.Exists(ATTACH))
                {
                    mapi.AddAttachment(ATTACH);
                }
                mapi.SendMailPopup(SUBJECT, BODY); // show E-mail before sending
                // mapi.SendMailDirect(SUBJECT, BODY); // or send E-mail directly
        }
    catch
        { 
        return; 
        }
    }
//################################################################################################
   public string Get_Project()
    {
	try
	{
		// Read selected Eplan project Path
		//==========================================
		Eplan.EplApi.ApplicationFramework.ActionManager oMngr = new Eplan.EplApi.ApplicationFramework.ActionManager();
		Eplan.EplApi.ApplicationFramework.Action oSelSetAction = oMngr.FindAction("selectionset");
		string sProjektT = "";
		if (oMngr != null)
		{
			Eplan.EplApi.ApplicationFramework.ActionCallingContext ctx = new Eplan.EplApi.ApplicationFramework.ActionCallingContext();
			ctx.AddParameter("TYPE", "PROJECT");
			bool sRet = oSelSetAction.Execute(ctx);

			if (sRet)
			{ctx.GetParameter("PROJECT",ref sProjektT);}
			//MessageBox.Show("Project: " + sProjektT);
		}
		return sProjektT;
    }
	catch
 		{return "";}
    }
//################################################################################################
    public string Get_Name(string sProj)
    {
        try
        {
            // Read selected Eplan project name
			int i = sProj.Length - 5;
            string sTemp = sProj.Substring(1, i);
            i= sTemp.LastIndexOf(@"\");
            sTemp = sTemp.Substring(i+1);
            //MessageBox.Show("Project name: " + sTemp);
            return sTemp;     
        }
        catch
        { return "ERROR"; }
    }
//################################################################################################   
    //MAPI Definition (Function is required to send emails)

    #region MAPI
    // MAPI: E-mail Classes
    public class MAPI
    {
        public bool AddRecipientTo(string email)
        {
            return AddRecipient(email, HowTo.MAPI_TO);
        }

        public bool AddRecipientCC(string email)
        {
            return AddRecipient(email, HowTo.MAPI_TO);
        }

        public bool AddRecipientBCC(string email)
        {
            return AddRecipient(email, HowTo.MAPI_TO);
        }

        public void AddAttachment(string strAttachmentFileName)
        {
            m_attachments.Add(strAttachmentFileName);
        }

        public int SendMailPopup(string strSubject, string strBody)
        {
            return SendMail(strSubject, strBody, MAPI_LOGON_UI
                | MAPI_DIALOG);
        }

        public int SendMailDirect(string strSubject, string strBody)
        {
            return SendMail(strSubject, strBody, MAPI_LOGON_UI);
        }


        [DllImport("MAPI32.DLL")]
        static extern int MAPISendMail(IntPtr sess, IntPtr hwnd,
            MapiMessage message, int flg, int rsv);

        int SendMail(string strSubject, string strBody, int how)
        {
            MapiMessage msg = new MapiMessage();
            msg.subject = strSubject;
            msg.noteText = strBody;

            msg.recips = GetRecipients(out msg.recipCount);
            msg.files = GetAttachments(out msg.fileCount);

            m_lastError = MAPISendMail(new IntPtr(0), new IntPtr(0),
                msg, how, 0);

            if (m_lastError > 1)
                MessageBox.Show("MAPISendMail failed! " + GetLastError(),
                    "MAPISendMail");

            Cleanup(ref msg);
            return m_lastError;
        }

        bool AddRecipient(string email, HowTo howTo)
        {
            MapiRecipDesc recipient = new MapiRecipDesc();

            recipient.recipClass = (int)howTo;
            recipient.name = email;
            m_recipients.Add(recipient);

            return true;
        }

        IntPtr GetRecipients(out int recipCount)
        {
            recipCount = 0;
            if (m_recipients.Count == 0)
                return IntPtr.Zero;

            int size = Marshal.SizeOf(typeof(MapiRecipDesc));
            IntPtr intPtr =
                Marshal.AllocHGlobal(m_recipients.Count * size);

            int ptr = (int)intPtr;
            foreach (MapiRecipDesc mapiDesc in m_recipients)
            {
                Marshal.StructureToPtr(mapiDesc, (IntPtr)ptr, false);
                ptr += size;
            }

            recipCount = m_recipients.Count;
            return intPtr;
        }

        IntPtr GetAttachments(out int fileCount)
        {
            fileCount = 0;
            if (m_attachments == null)
                return IntPtr.Zero;

            if ((m_attachments.Count <= 0) || (m_attachments.Count >
                maxAttachments))
                return IntPtr.Zero;

            int size = Marshal.SizeOf(typeof(MapiFileDesc));
            IntPtr intPtr =
                Marshal.AllocHGlobal(m_attachments.Count * size);

            MapiFileDesc mapiFileDesc = new MapiFileDesc();
            mapiFileDesc.position = -1;
            int ptr = (int)intPtr;

            foreach (string strAttachment in m_attachments)
            {
                mapiFileDesc.name = Path.GetFileName(strAttachment);
                mapiFileDesc.path = strAttachment;
                Marshal.StructureToPtr(mapiFileDesc, (IntPtr)ptr, false);
                ptr += size;
            }

            fileCount = m_attachments.Count;
            return intPtr;
        }

        void Cleanup(ref MapiMessage msg)
        {
            int size = Marshal.SizeOf(typeof(MapiRecipDesc));
            int ptr = 0;

            if (msg.recips != IntPtr.Zero)
            {
                ptr = (int)msg.recips;
                for (int i = 0; i < msg.recipCount; i++)
                {
                    Marshal.DestroyStructure((IntPtr)ptr,
                        typeof(MapiRecipDesc));
                    ptr += size;
                }
                Marshal.FreeHGlobal(msg.recips);
            }

            if (msg.files != IntPtr.Zero)
            {
                size = Marshal.SizeOf(typeof(MapiFileDesc));

                ptr = (int)msg.files;
                for (int i = 0; i < msg.fileCount; i++)
                {
                    Marshal.DestroyStructure((IntPtr)ptr,
                        typeof(MapiFileDesc));
                    ptr += size;
                }
                Marshal.FreeHGlobal(msg.files);
            }

            m_recipients.Clear();
            m_attachments.Clear();
            m_lastError = 0;
        }

        public string GetLastError()
        {
            if (m_lastError <= 26)
                return errors[m_lastError];
            return "MAPI error [" + m_lastError.ToString() + "]";
        }

        readonly string[] errors = new string[] {
        "OK [0]", "User abort [1]", "General MAPI failure [2]", 
                "MAPI login failure [3]", "Disk full [4]", 
                "Insufficient memory [5]", "Access denied [6]", 
                "-unknown- [7]", "Too many sessions [8]", 
                "Too many files were specified [9]", 
                "Too many recipients were specified [10]", 
                "A specified attachment was not found [11]",
        "Attachment open failure [12]", 
                "Attachment write failure [13]", "Unknown recipient [14]", 
                "Bad recipient type [15]", "No messages [16]", 
                "Invalid message [17]", "Text too large [18]", 
                "Invalid session [19]", "Type not supported [20]", 
                "A recipient was specified ambiguously [21]", 
                "Message in use [22]", "Network failure [23]",
        "Invalid edit fields [24]", "Invalid recipients [25]", 
                "Not supported [26]" 
        };


        List<MapiRecipDesc> m_recipients = new
            List<MapiRecipDesc>();
        List<string> m_attachments = new List<string>();
        int m_lastError = 0;

        const int MAPI_LOGON_UI = 0x00000001;
        const int MAPI_DIALOG = 0x00000008;
        const int maxAttachments = 20;

        enum HowTo { MAPI_ORIG = 0, MAPI_TO, MAPI_CC, MAPI_BCC };
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class MapiMessage
    {
        public int reserved;
        public string subject;
        public string noteText;
        public string messageType;
        public string dateReceived;
        public string conversationID;
        public int flags;
        public IntPtr originator;
        public int recipCount;
        public IntPtr recips;
        public int fileCount;
        public IntPtr files;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class MapiFileDesc
    {
        public int reserved;
        public int flags;
        public int position;
        public string path;
        public string name;
        public IntPtr type;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class MapiRecipDesc
    {
        public int reserved;
        public int recipClass;
        public string name;
        public string address;
        public int eIDSize;
        public IntPtr entryID;
    }
    #endregion

}
