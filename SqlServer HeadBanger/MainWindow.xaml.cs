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
            ResToHtml.Visibility = Visibility.Hidden;
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
        
        private void Button_Click(object sender, RoutedEventArgs e)
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
                    foreach (var item in DRow.ItemArray) // Loop over the items.
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

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            trlbl.Content = "Threads: (" + sldr.Value.ToString() + ")";
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
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            RsltTable = new List<Data>();
            SpidList = new List<Spids>();
            var server = srvTB.Text.Trim();
            var db = dbTB.Text.Trim();
            var userId = unTB.Text.Trim();
            var password = pwdTB.Password.Trim();
            var tsql = StringFromRTB(TSQLTB);
            var TimeOut = TimeOutTB.Value.ToString();
            var tasks = new Task[(int)sldr.Value];
            var RandStart = RND_st.Text.Trim();
            var RandEnd = RND_end.Text.Trim();
            var ST = DateTime.Now;
            
            for (int i = 0; i < sldr.Value; i++)
            {
                tasks[i] = Task.Run(new Action(() => myMethod(server, db, userId, password, tsql, TimeOut, RandStart, RandEnd)));
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
                RSLT_lbl.Content = "Total threads [" + SpidList.Count.ToString() + "] | Unique SPID's [" + x.Count().ToString() + "] | Duration [" + dateDifference.ToString()+ "]";
            }
            catch (Exception exp)
            {

                RSLT_lbl.Content = "Error: " + exp.Message.ToString();
            }
            ResToHtml.Visibility = Visibility.Visible;
        }



        private void myMethod(string server, string db, string userId, string password, string tsql, string TimeOut, string RandStart, string RandEnd)
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

                    connection.StatisticsEnabled = true;
                    connection.Open();
                    command.ExecuteNonQuery();
                    var res = command.ExecuteScalar();
                    var spid = res.ToString();
                    SqlCommand cm = new SqlCommand(CodeStr, connection);
                    int RA = 0;
                    var starttime = DateTime.Now;
                    try
                    {
                        RA = cm.ExecuteNonQuery();
                        var endtime = DateTime.Now;
                        var duration = endtime - starttime;
                        string Xinf = "";
                        IDictionary currentStatistics = connection.RetrieveStatistics();
                            
                        long bytesReceived = (long) currentStatistics["BytesReceived"];
                        long bytesSent = (long) currentStatistics["BytesSent"];
                        long selectCount = (long) currentStatistics["SelectCount"];
                        long selectRows = (long) currentStatistics["SelectRows"];
                        long ConnectionTime = (long) currentStatistics["ConnectionTime"];
                        long IduRows = (long)currentStatistics["IduRows"];
                        long NetworkServerTime = (long)currentStatistics["NetworkServerTime"];
                        long Transactions = (long)currentStatistics["Transactions"];

                        Xinf = 
                        "BytesReceived: " + bytesReceived.ToString()+ Environment.NewLine +
                        "BytesSent: " + bytesSent.ToString() + Environment.NewLine +
                        "SelectCount: " + selectCount.ToString() + Environment.NewLine +
                        "SelectRows: " + selectRows.ToString() + Environment.NewLine + 
                        "ConnectionTime: " + ConnectionTime.ToString() + Environment.NewLine + 
                        "IduRows: " + IduRows.ToString() + Environment.NewLine +
                        "NetworkServerTime: " + NetworkServerTime.ToString() + Environment.NewLine +
                        "Transactions: " + Transactions.ToString();




                        RsltTable.Add(new Data
                        {
                            SPId = spid,
                            Tsql = CodeStr,
                            StartTime = starttime.ToString("HH:mm:ss:fff"),
                            EndTime = endtime.ToString("HH:mm:ss:fff"),
                            Duration = ((long)duration.TotalMilliseconds).ToString(),
                            RowsAffected = IduRows,
                            State = "Success",
                            BytesReceived = bytesReceived,
                            BytesSent = bytesSent,
                            SelectCount = selectCount,
                            SelectRows = selectRows,
                            ConnectionTime = ConnectionTime,
                            NetworkServerTime =NetworkServerTime,
                            Transactions =Transactions
                        });
                        SpidList.Add(new Spids
                        {
                            SPId = spid
                        });




                    }
                    catch (Exception e)
                    {
                        if (RsltTable != null)
                        {
                            RsltTable.Add(new Data
                            {
                                SPId = spid,
                                Tsql = CodeStr,
                                StartTime = " -- ",
                                EndTime = " -- ",
                                Duration = "0",
                                RowsAffected = 0,
                                State = e.Message.ToString(),
                                BytesReceived = 0,
                                BytesSent = 0,
                                SelectCount = 0,
                                SelectRows = 0,
                                ConnectionTime = 0,
                                NetworkServerTime = 0,
                                Transactions = 0
                            });
                        }

                        
                        
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
            public string Duration { get; set; }
            public long RowsAffected { get; set; }
            public string State { get; set; }
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
            public long SelectCount { get; set; }
            public long SelectRows { get; set; }
            public long ConnectionTime { get; set; }
            public long NetworkServerTime { get; set; }
            public long Transactions { get; set; }
        }
        private class Spids
        {
            public string SPId { get; set; }
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
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
<td class=""tblhdr"">BytesReceived</td>
<td class=""tblhdr"">BytesSent</td>
<td class=""tblhdr"">SelectCount</td>
<td class=""tblhdr"">SelectRows</td>
<td class=""tblhdr"">ConnectionTime</td>
<td class=""tblhdr"">NetworkServerTime</td>
<td class=""tblhdr"">Transactions</td>
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
                        REC.BytesReceived + COL +
                        REC.BytesSent + COL +
                        REC.SelectCount + COL +
                        REC.SelectRows + COL +
                        REC.ConnectionTime + COL +
                        REC.NetworkServerTime + COL +
                        REC.Transactions + END_ROW + Environment.NewLine;
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

                RSLT_lbl.Content = "Error: " + exp.Message.ToString();
            }
           

        }


    }
}