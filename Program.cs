using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Common.ThreadManagement;
using Cmf.CustomerPortal.Orchestration.ThreadManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace KnowledgeBaseToCSV
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string basePath;
            var folderBrowserDialog = new FolderBrowserDialog();

            // Show the FolderBrowserDialog.
            DialogResult result2 = folderBrowserDialog.ShowDialog();
            if (result2 == DialogResult.OK)
            {
                basePath = folderBrowserDialog.SelectedPath + '\\';
            }
            else
            {
                basePath = null;
                throw new Exception("Folder not found");
            }

            GetThreadsByFilterInput getThreadsByfilterInput = new GetThreadsByFilterInput();
            getThreadsByfilterInput.PageSize = 100;
            getThreadsByfilterInput.PageNumber = 1;
            getThreadsByfilterInput.ThreadType = ThreadType.KnowledgeBase;

            NgpDataSet resultThreads = getThreadsByfilterInput.GetThreadsByFilterSync().Threads;
            DataSet dataSetThreads = Utilities.ToDataSet(resultThreads);
            string pathThreads = basePath + "threads.csv";

            string[] threadColumns = {"id", "title", "type", "createdOn", "tags", "owner" };

            foreach (DataTable table in dataSetThreads.Tables)
            {
                Utilities.ToCSV(table.DefaultView.ToTable(false, threadColumns), pathThreads);
            }

            string[] articleColumns = { "Body", "CreatedOn", "CreatedBy", "From", "IsMVArticle", "numberOfVotes"};

            if (dataSetThreads.Tables.Count > 0)
            {
                string pathArticles;
                List<object> threadIds = Utilities.GetColumnValues(dataSetThreads.Tables[0], "id");
                foreach (long threadId in threadIds)
                {
                    GetAllArticlesByPageInput getAllArticlesByPageInput = new GetAllArticlesByPageInput();
                    getAllArticlesByPageInput.ThreadId = threadId;
                    getAllArticlesByPageInput.ArticlesSortingCriteria = ArticlesSortingCriteria.MostVoted;

                    NgpDataSet resultArticles = getAllArticlesByPageInput.GetAllArticlesByPageSync().Articles;
                    DataSet dataSetArticles = Utilities.ToDataSet(resultArticles);

                    if (dataSetArticles.Tables.Count > 0)
                    {
                        DataTable dataTableMainArt = dataSetArticles.Tables[0];

                        List<object> threadIdParent = Utilities.GetColumnValues(dataTableMainArt, "ArticleId");
                        pathArticles = basePath + "Article_" + threadId + '_' + threadIdParent[0] + ".csv";

                        Utilities.ToCSV(dataTableMainArt.DefaultView.ToTable(false, articleColumns), pathArticles);
                    }
                }
            }
            MessageBox.Show("Conversion to CSV finished", "Prompt", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }
    }
}





