using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Web.Script.Serialization;

namespace SqlServer_HeadBanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Data> RsltTable;
        List<Spids> SpidList;

        public MainWindow()
        {
            InitializeComponent();
            DisableStartButton();
            TimeOutTB.Value = 30;
            RadioButtonVisible(2);
        }
        private void DisableStartButton()
        {
            btnstrt.Visibility = Visibility.Hidden;
            editBTN.Visibility = Visibility.Hidden;
            btntstcon.Visibility = Visibility.Visible;
            srvTB.IsEnabled = true;
            dbTB.IsEnabled = true;
            unTB.IsEnabled = true;
            pwdTB.IsEnabled = true;
        }
        private void EnableStartButton()
        {
            btnstrt.Visibility = Visibility.Visible;
            editBTN.Visibility = Visibility.Visible;
            btntstcon.Visibility = Visibility.Hidden;
            srvTB.IsEnabled = false;
            dbTB.IsEnabled = false;
            unTB.IsEnabled = false;
            pwdTB.IsEnabled = false;
            editBTN.IsEnabled = true;
        }

        private void ConnectBTN_CLK(object sender, RoutedEventArgs e)
        {
            try
            {
                btntstcon.Content = "Testing...";
                DataTable results = new DataTable();
                using (var connection = new SqlConnection("Server=" + srvTB.Text.Trim() + ";Database=" + dbTB.Text.Trim() + ";User Id=" + unTB.Text.Trim() + ";Password=" + pwdTB.Password.Trim() + ";"))
                {
                    using (SqlCommand command = new SqlCommand("select @@spid", connection))
                    using (SqlDataAdapter dataAdapter = new SqlDataAdapter(command))
                        dataAdapter.Fill(results);
                }
                foreach (DataRow DRow in results.Rows)
                {
                    foreach (var item in DRow.ItemArray)
                    {
                        spidLBL.Content = "@@SPID: " + item.ToString();
                    }
                }

                btntstcon.Content = "Connected";
                EnableStartButton();

            }

            catch (Exception err)
            {
                spidLBL.Content = err.Message;
                btntstcon.Content = "Faild! (retry)";
                DisableStartButton();
            }

        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {

            btntstcon.Content = "Disconnected! (connect)";
            DisableStartButton();
        }

        private void TimeOutTB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TI_lbl.Content = "Timeout: (" + TimeOutTB.Value.ToString() + ")";
        }
        string StringFromRTB(RichTextBox TSQLTB)
        {
            TextRange textRange = new TextRange(
                TSQLTB.Document.ContentStart,
                TSQLTB.Document.ContentEnd
            );
            return textRange.Text;
        }
        public string ConvertDataTabletoJson(DataTable dt)
        {
                    System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                    Dictionary<string, object> row;
                    foreach (DataRow dr in dt.Rows)
                    {
                        row = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            row.Add(col.ColumnName, dr[col]);
                        }
                        rows.Add(row);
                    }
                    return serializer.Serialize(rows);
        }


        public string ConvertDataTableToString(DataTable dt)
        {
            int cc = dt.Columns.Count;
            string res ="", ls = "";
            int ci = 0;
            string sep = new String('-', cc * 10);
            foreach (DataColumn dc in dt.Columns)
            {
                ls = "  |";
                if (ci == cc - 1)
                    {
                        ls = Environment.NewLine + sep + Environment.NewLine;
                    }
                res += dc.ColumnName.ToString() + ls;
                ci++;
            }
            foreach (DataRow dr in dt.Rows)
            {
                ls = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;

                for (int i = 0; i < cc; i++)
                {
                    if (i == cc - 1)
                    {
                        ls = Environment.NewLine;
                    }
                    if (dr[i].ToString() != null || dr[i].ToString().Length > 0)
                    {
                        res += dr[i].ToString() + ls;
                    }
                    else
                    {
                        res += "[NULL]" + ls;
                    }

                }
                res  += Environment.NewLine; 
            }
            return res;
        }
        private void StartTheMagic(object sender, RoutedEventArgs e)
        {
            int TasksAmount = 0;
            try
            {
                TasksAmount = int.Parse(ThreadsIntTB.Text);
            }
            catch (Exception)
            {

                MessageBox.Show(ThreadsIntTB.Text + " is not a valid threads number!");
            }
            
            int.TryParse(ThreadsIntTB.Text, out TasksAmount);

            if (TasksAmount >= 0)
            {
                int ResFormat = 0;
                if (ResInclude.IsChecked == true)
                {
                    if (RB2_ToString.IsChecked == true)
                    {
                        ResFormat = 1; 
                    }
                    else if (RB_ToJson.IsChecked == true)
	                {
                        ResFormat = 2;
	                }
                } 
                RsltTable = new List<Data>();
                SpidList = new List<Spids>();
                var server = srvTB.Text.Trim();
                var db = dbTB.Text.Trim();
                var userId = unTB.Text.Trim();
                var password = pwdTB.Password.Trim();
                var tsql = StringFromRTB(TSQLTB);
                var TimeOut = TimeOutTB.Value.ToString();
                var tasks = new Task[(int)TasksAmount];
                var RandStart = RND_st.Text.Trim();
                var RandEnd = RND_end.Text.Trim();
                var ST = DateTime.Now;

                for (int i = 0; i < TasksAmount; i++)
                {
                    tasks[i] = Task.Run(new Action(() => myMethod(server, db, userId, password, tsql, TimeOut, RandStart, RandEnd,ResFormat)));
                }
                Task.WaitAll(tasks);
                var ET = DateTime.Now;
                TimeSpan dateDifference = ET.Subtract(ST);

                dg.ItemsSource = null;
                dg.ItemsSource = RsltTable;

                var g = SpidList.GroupBy(i => i);
                g.Count().ToString();
                try
                {
                    var x = from spidt in SpidList
                            group spidt by new { spid = spidt.SPId } into gcollection
                            select new { Count = gcollection.Count() };
                    RSLT_lbl.Content = "Total threads [" + SpidList.Count.ToString() + "] | Unique SPID's [" + x.Count().ToString() + "] | Duration [" + dateDifference.ToString() + "]";
                }
                catch (Exception exp)
                {

                    RSLT_lbl.Content = "Error: " + exp.Message.ToString();
                } 
            }
           
        }

        private void RegData(string SPId, string Tsql, string StartTime, string EndTime, long Duration, long RowsAffected, string State, long BytesSent, long BytesReceived, long SelectCount,
                                 long SelectRows,
                                 long ConnectionTime,
                                 long NetworkServerTime,
                                 long Transactions,
                                 string runres)
        {
            if (RsltTable != null)
            {
                RsltTable.Add(new Data
                {
                    SPId = SPId,
                    Tsql = Tsql,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    Duration = Duration,
                    RowsAffected = RowsAffected,
                    State = State,
                    BytesSent = BytesSent,
                    BytesReceived = BytesReceived,
                    SelectCount = SelectCount,
                    SelectRows = SelectRows,
                    ConnectionTime = ConnectionTime,
                    NetworkServerTime = NetworkServerTime,
                    Transactions = Transactions,
                    RunResults = runres
                });
            }
            try
            {
                if (SpidList != null)
                {
                    SpidList.Add(new Spids
                    {
                        SPId = SPId
                    });
                }
            }
            catch (Exception exp)
            {
                RSLT_lbl.Content = "Error: " + exp.Message.ToString();
            }
            
        }

        private void myMethod(string server, string db, string userId, string password, string tsql, string TimeOut, string RandStart, string RandEnd,int ResultFormat)
        {

            DataTable results = new DataTable();
            using (var connection = new SqlConnection("Server=" + server + ";Database=" + db + ";User Id=" + userId + ";Password=" + password + ";Connection Timeout=" + TimeOut + ";"))
            {
                using (SqlCommand command = new SqlCommand("select @@SPID", connection))
                {
                    Random rnd = new Random();
                    string CodeStr = tsql;
                    int RND_A = 0;
                    int RND_B = 1000;
                    Int32.TryParse(RandStart, out RND_A);
                    Int32.TryParse(RandEnd, out RND_B);
                    int RandInt = rnd.Next(RND_A, RND_B);
                    CodeStr = tsql.Replace("{RAND_INT}", RandInt.ToString());
                    string pattern = "(\\/\\*([\0-\uffff]*?)\\*\\/)|(--.*)";
                    Regex rgx = new Regex(pattern);
                    string ClearTsql = rgx.Replace(CodeStr, "").Trim().ToString();
                    connection.StatisticsEnabled = true;
                    connection.Open();
                    var res = command.ExecuteScalar();
                    var spid = res.ToString();
                    SqlCommand cm = new SqlCommand(CodeStr, connection);
                    string runres = "";
                    var starttime = DateTime.Now;
                    string StartTime = starttime.ToString("HH:mm:ss:fff");
                    try
                    {
                        DataSet ds = new DataSet();
                        DataTable table = new DataTable();
                        SqlDataAdapter a = new SqlDataAdapter(CodeStr, connection);
                        a.Fill(ds);
                        if (ResultFormat == 1 || ResultFormat == 2)
                        {
                            int tb_serial = 1;
                            foreach (DataTable tbl in ds.Tables)
                            {

                                runres += "============ Result #[" + tb_serial.ToString() + "] ============" + Environment.NewLine;
                                if (ResultFormat == 1) //string
                                {
                                    runres += ConvertDataTableToString(tbl);
                                }
                                else if (ResultFormat == 2) //json
                                {
                                    runres += ConvertDataTabletoJson(tbl);
                                }

                                runres += Environment.NewLine;
                                tb_serial++;
                            }  
                        }
                        
                        var endtime = DateTime.Now;
                        var duration = endtime - starttime;
                        IDictionary currentStatistics = connection.RetrieveStatistics();

                        long bytesReceived = (long)currentStatistics["BytesReceived"];
                        long bytesSent = (long)currentStatistics["BytesSent"];
                        long selectCount = (long)currentStatistics["SelectCount"];
                        long selectRows = (long)currentStatistics["SelectRows"];
                        long ConnectionTime = (long)currentStatistics["ConnectionTime"];
                        long IduRows = (long)currentStatistics["IduRows"];
                        long NetworkServerTime = (long)currentStatistics["NetworkServerTime"];
                        long Transactions = (long)currentStatistics["Transactions"];
                        string EndTime = endtime.ToString("HH:mm:ss:fff");
                        long Duration = ((long)duration.TotalMilliseconds);
                        string RunResults = runres;
                        RegData(spid, ClearTsql, StartTime, EndTime, Duration, IduRows, "Success", bytesSent, bytesReceived, selectCount, selectRows, ConnectionTime, NetworkServerTime, Transactions, runres);
                    }
                    catch (Exception e)
                    {

                        string errmsgstr = e.Message.ToString();
                        RegData(spid, ClearTsql, StartTime, " -- ", 0, 0, errmsgstr, 0, 0, 0, 0, 0, 0, 0,"");
                    }

                    connection.Close();
                    if (connection != null)
                    {
                        connection.Dispose();
                    }
                }
            }
        }

        private class Data
        {
            public string SPId { get; set; }
            public string Tsql { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public long Duration { get; set; }
            public long RowsAffected { get; set; }
            public string State { get; set; }
            public long BytesSent { get; set; }
            public long BytesReceived { get; set; }
            public long SelectCount { get; set; }
            public long SelectRows { get; set; }
            public long ConnectionTime { get; set; }
            public long NetworkServerTime { get; set; }
            public long Transactions { get; set; }
            public string RunResults { get; set; }
            public override string ToString()
            {
                string ls = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;

                return
                    SPId.ToString() + ls +
                    Tsql + ls +
                    StartTime + ls +
                    EndTime + ls +
                    Duration + ls +
                    RowsAffected + ls +
                    State + ls +
                    BytesSent + ls +
                    BytesReceived + ls +
                    SelectCount + ls +
                    SelectRows + ls +
                    ConnectionTime + ls +
                    NetworkServerTime + ls +
                    Transactions + ls +
                    "Thread results are not supported in Csv!";
                

            }
        }
        private class Spids
        {
            public string SPId { get; set; }
        }
        private void ResultsToHtmlRP(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string FileUniqueName = "\\HeadBangingResult_" + DateTime.Now.ToBinary().ToString().Replace("-", "") + ".html";
                string content = @"
<!--Created By Shimon Gb's SqlServerHeadBanger-->
<HTML>
<head>
<title>SqlServer HeadBanging Report</title>
<meta charset=""UTF-8"">
</head>
<BODY style=""font-family: 'Trebuchet MS', Helvetica, sans-serif;color:#535a60;"">
<style>
	.failed {
	color:#b82929;
	}
	.Row_tbl {
	color:#535a60;
	font-weight = 800;
	}
	.tbl {
	width:100%;
	padding: 1%;
    border-collapse: collapse;
	}
	.tbl:hover {
	  cursor:default;
	 }
	.tblhdr {
	background-color: #6600FF;
	color: #FFFFFF;
	}
.tbl th, td{
	border: 1px solid #6600FF;
    padding: 1%;
	}
</style>
    <div style=""color: #6600FF;width:100%;height:10%;padding:1%;"">
        <h1>SqlServer HeadBanging Report</h1>
    </div>
    <TABLE class=""tbl"">
    <tr>
        <td class=""tblhdr"">Spid</td>
        <td class=""tblhdr"">Tsql</td>
        <td class=""tblhdr"">StartTime</td>
        <td class=""tblhdr"">EndTime</td>
        <td class=""tblhdr"">Duration</td>
        <td class=""tblhdr"">RowsAffected</td>
        <td class=""tblhdr"">State</td>
        <td class=""tblhdr"">BytesSent</td>
        <td class=""tblhdr"">BytesReceived</td>
        <td class=""tblhdr"">SelectCount</td>
        <td class=""tblhdr"">SelectRows</td>
        <td class=""tblhdr"">ConnectionTime</td>
        <td class=""tblhdr"">NetworkServerTime</td>
        <td class=""tblhdr"">Transactions</td>
        <td class=""tblhdr"">RunResults</td>
    </tr>

";
                string OPN_ROW = @"<tr class=""$CLASS$""><td>";
                string END_ROW = "</td></tr>";
                string COL = "</td><td>";
                foreach (Data REC in RsltTable)
                {
                    if (REC.State == "Error!")
                    {
                        OPN_ROW = OPN_ROW.Replace("$CLASS$", "failed");
                    }
                    else
                    {
                        OPN_ROW = OPN_ROW.Replace("$CLASS$", "Row_tbl");
                    }
                    content += OPN_ROW + REC.SPId.ToString() + COL +
                        REC.Tsql.ToString() + COL +
                        REC.StartTime.ToString() + COL +
                        REC.EndTime.ToString() + COL +
                        REC.Duration.ToString() + COL +
                        REC.RowsAffected + COL +
                        REC.State.ToString() + COL +
                        REC.BytesSent + COL +
                        REC.BytesReceived + COL +
                        REC.SelectCount + COL +
                        REC.SelectRows + COL +
                        REC.ConnectionTime + COL +
                        REC.NetworkServerTime + COL +
                        REC.Transactions + COL +
                        REC.RunResults.Replace(Environment.NewLine,"<br/>") + END_ROW + Environment.NewLine;
                }
                content += @"   </TABLE>
    <h3>Did you enjoyed the almighty HeadBanger? so go and tell **ALL** your friends about it!</h3>
</BODY>
</HTML>";
                System.IO.File.WriteAllText(path + FileUniqueName, content);
                MessageBox.Show("HTML Report was generated successfully!");
            }
            catch (Exception exp)
            {

                if (dg.ItemsSource == null)
                {
                    RSLT_lbl.Content = "Error: No data exists in grid!";
                }
                else
                {
                    RSLT_lbl.Content = "Error: " + exp.Message.ToString();
                }

            }


        }

        private void ResToCsv_run(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string FileUniqueName = "\\HeadBangingResult_" + DateTime.Now.ToBinary().ToString().Replace("-", "") + ".csv";
                StreamWriter sw = new StreamWriter(path + FileUniqueName, false);
                string ls = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                string headerLine = "Spid" + ls +
                        "Tsql" + ls +
                        "StartTime" + ls +
                        "EndTime" + ls +
                        "Duration" + ls +
                        "RowsAffected" + ls +
                        "State" + ls +
                        "BytesSent" + ls +
                        "BytesReceived" + ls +
                        "SelectCount" + ls +
                        "SelectRows" + ls +
                        "ConnectionTime" + ls +
                        "NetworkServerTime" + ls +
                        "Transactions" + ls +
                        "RunResults" ;
                sw.WriteLine(headerLine);
                int iColCount = 14;
                foreach (Data dt in dg.ItemsSource)
                {
                    sw.WriteLine(dt.ToString());
                }
                sw.Close();
                MessageBox.Show("Csv Report was generated successfully!");
            }
            catch (Exception exp)
            {

                RSLT_lbl.Content = "Error: " + exp.Message.ToString();
            }

        }

        private void CopyAllWithHDR(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dg.ItemsSource != null)
                {
                    dg.SelectAllCells();
                    dg.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                    ApplicationCommands.Copy.Execute(null, dg);
                    dg.UnselectAllCells();

                }
                MessageBox.Show("Data copied to clipboard successfully!");
            }
            catch (Exception exp)
            {

                RSLT_lbl.Content = "Error: " + exp.Message.ToString();
            }

        }

        private void GetRes_Checked(object sender, RoutedEventArgs e)
        {
            RadioButtonVisible(1);
        }

        private void RadioButtonVisible(int act)
        {
            if (act == 1 ) //visible
            {
                RB_ToJson.Visibility = Visibility.Visible;
                RB2_ToString.Visibility = Visibility.Visible; 
            }
            else
            {
                RB_ToJson.Visibility = Visibility.Hidden;
                RB2_ToString.Visibility = Visibility.Hidden;
            }
            
        }

        private void GetRes_Unchecked(object sender, RoutedEventArgs e)
        {
            RadioButtonVisible(2);
        }

    }
}