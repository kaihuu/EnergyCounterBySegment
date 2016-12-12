using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Device.Location;

namespace EnergyCounterBySegment
{
    public partial class EnergyCounter : Form
    {
        public EnergyCounter()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            //if(textBox1.Text == null)
            //{
            //    MessageBox.Show("ERROR", "TRIP_IDを入力してください", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}
            int[] tripId = new int[] {8350,8352,8353,8354,8355,8356,8357,8358}; 
                //Convert.ToInt32(textBox1.Text);

            //対象リンクを増やす場合、その対象リンクをList<DataTable> dtに格納する
            List<DataTable> dt = new List<DataTable>();//道路リンク格納
            int[] id = new int[] { 220 };
            DataTable tempTable = LinkDateGetter.LinkTableGetter(id);//復路マップマッチング(富井先生：代官山下ルート)
            tempTable.TableName = "富井先生,復路,代官山下ルート";
            dt.Add(tempTable);

            id = new int[] { 224 };
            tempTable = LinkDateGetter.LinkTableGetter(id);//復路マップマッチング(富井先生：代官山上ルート)
            tempTable.TableName = "富井先生,復路,代官山上ルート";
            dt.Add(tempTable);

            id = new int[] { 221 };
            tempTable = LinkDateGetter.LinkTableGetter(id);//往路マップマッチング(富井先生：小学校上ルート)
            tempTable.TableName = "富井先生,往路,小学校上ルート";
            dt.Add(tempTable);

            id = new int[] { 225 };
            tempTable = LinkDateGetter.LinkTableGetter(id);//往路マップマッチング(富井先生：小学校下ルート)
            tempTable.TableName = "富井先生,往路,小学校下ルート";
            dt.Add(tempTable);
            List<List<LinkData>> linkList = new List<List<LinkData>>();
            int[] semanticLinks = new int[] { 220, 224, 221, 225 };
            for (int i = 0; i < semanticLinks.Length; i++)
            {
                linkList.Add(generateLinkList(dt[i], semanticLinks[i]));
            } 
 
            for (int s = 0; s < tripId.Length; s++)
            {
                double[] sumDist = new double[dt.Count];
                double[] maxDist = new double[dt.Count];

                List<List<GidsDifferenceData>> tempArray = new List<List<GidsDifferenceData>>();

                for (int n = 0; n < dt.Count; n++)
                {
                    tempArray.Add(new List<GidsDifferenceData>());
                    dt[n].DefaultView.Sort = "START_LAT,START_LONG";
                }
                //GIDｓデータ読み込み
                DataTable GidsDataTrip = LinkDateGetter.TripGidsGetterfromTRIPID(tripId[s]);
                GidsDataTrip.DefaultView.Sort = "JST";
                DataRow[] GidsRowsTrip = GidsDataTrip.Select(null, "JST");
                for (int i = 0; i < GidsRowsTrip.Length; i++)
                {
                    GidsData tempData = new GidsData(tripId[s], Convert.ToDateTime(GidsRowsTrip[i]["JST"]), Convert.ToDouble(GidsRowsTrip[i]["LATITUDE"]),
                        Convert.ToDouble(GidsRowsTrip[i]["LONGITUDE"]), Convert.ToDouble(GidsRowsTrip[i]["DELTA_GIDS"]));
                    for (int n = 0; n < dt.Count; n++)
                    {

                        double tempDist = searchNearestLink(linkList[n], tempData, tempArray[n], semanticLinks[n], tripId[s]);
                        sumDist[n] += tempDist;
                        if (tempDist > maxDist[n]) maxDist[n] = tempDist;
                    }
                    StateLabel.Text = i + "/" + (GidsRowsTrip.Length - 1) + " " + s + "/" + (tripId.Length - 1);
                    StateLabel.Update();
                    Application.DoEvents();
                }
                int element = getMinElement(sumDist);
             //   List<GidsDifferenceData> result = tempArray[element];

                if ((sumDist[element] <= 0.5 && maxDist[element] <= 0.003))
                {
                    List<ConsumedElectricEnergyData> resultConsumed = new List<ConsumedElectricEnergyData>();

                    DataTable ECOLOGDataTrip = LinkDateGetter.TripECOLOGGetterfromTRIPID(tripId[s]);
                    ECOLOGDataTrip.DefaultView.Sort = "JST";
                    DataRow[] ECOLOGRowsTrip = ECOLOGDataTrip.Select(null, "JST");
                    List<ConsumedElectricEnergyData> tempArrayECOLOG = new List<ConsumedElectricEnergyData>();
                    for (int i = 0; i < ECOLOGRowsTrip.Length; i++)
                    {
                        ECOLOGData tempData = new ECOLOGData(tripId[s], Convert.ToDateTime(ECOLOGRowsTrip[i]["JST"]), Convert.ToDouble(ECOLOGRowsTrip[i]["LATITUDE"]),
                            Convert.ToDouble(ECOLOGRowsTrip[i]["LONGITUDE"]), Convert.ToDouble(ECOLOGRowsTrip[i]["CONSUMED_ELECTRIC_ENERGY"]));

                        double tempDist = searchNearestLinkforECOLOG(linkList[element], tempData, tempArrayECOLOG, semanticLinks[element], tripId[s]);
                        sumDist[element] += tempDist;
                        if (tempDist > maxDist[element]) maxDist[element] = tempDist;

                        StateLabel.Text = i + "/" + (ECOLOGRowsTrip.Length - 1) + " " + s + "/" + (tripId.Length - 1);
                        StateLabel.Update();
                        Application.DoEvents();
                    }
                    //インサート
            //        DatabaseInserter.InsertGidsDifference(result);
                    DatabaseInserter.InsertConsumedElectricEnergy(tempArrayECOLOG);
                }


            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        List<LinkData> generateLinkList(DataTable dt, int semanticLinkId)
        {
            ////近傍リンクの絞込
            //string query = "START_LAT > " + (gids.latitude - 0.05);
            //query += " AND START_LAT < " + (gids.latitude + 0.05);
            //query += " AND START_LONG > " + (gids.longitude - 0.05);
            //query += " AND START_LONG < " + (gids.longitude + 0.05);
            //DataRow[] dataRows = dt.Select(query);
            int startNum = 0;
            //リンクデータを往路復路で分岐させてソート
            if (semanticLinkId == 220 || semanticLinkId == 224)
            {
                startNum = 192180;
            }
            else if (semanticLinkId == 221 || semanticLinkId == 225)
            {
                startNum = 1591573;
            }
            DataTable LinkTable = LinkDateGetter.LinkTableGetter2(semanticLinkId);
            LinkTable.DefaultView.Sort = "NUM";
            DataRow[] LinkRows = LinkTable.Select(null, "NUM");
            DataRow[] StartLink = LinkTable.Select("NUM = " + startNum);
            List<LinkData> linkList = new List<LinkData>();

            linkList.Add(new LinkData(Convert.ToString(StartLink[0]["LINK_ID"]), Convert.ToInt32(StartLink[0]["NUM"]),
                Convert.ToDouble(StartLink[0]["START_LAT"]), Convert.ToDouble(StartLink[0]["START_LONG"]),
                Convert.ToDouble(StartLink[0]["END_LAT"]), Convert.ToDouble(StartLink[0]["END_LONG"]), Convert.ToDouble(StartLink[0]["DISTANCE"])));
            //スタート地点のリンクを初期値に設定

            Boolean flag = true;
            int j = 0;
            while (flag)
            {
                flag = false;
                for (int i = 0; i < LinkRows.Length; i++)
                {
                    if (Convert.ToDouble(LinkRows[i]["START_LAT"]) == linkList[j].END_LAT && Convert.ToDouble(LinkRows[i]["START_LONG"]) == linkList[j].END_LONG
                        && (Convert.ToDouble(LinkRows[i]["END_LAT"]) != linkList[j].START_LAT || Convert.ToDouble(LinkRows[i]["END_LONG"]) != linkList[j].START_LONG))
                    {

                        linkList.Add(new LinkData(Convert.ToString(LinkRows[i]["LINK_ID"]), Convert.ToInt32(LinkRows[i]["NUM"]),
                Convert.ToDouble(LinkRows[i]["START_LAT"]), Convert.ToDouble(LinkRows[i]["START_LONG"]),
                Convert.ToDouble(LinkRows[i]["END_LAT"]), Convert.ToDouble(LinkRows[i]["END_LONG"]), Convert.ToDouble(LinkRows[i]["DISTANCE"])));
                        j++;
                        flag = true;
                        break;
                    }
                }

            }
            return linkList;
        } 
        double searchNearestLink(List<LinkData> linkList, GidsData gids, List<GidsDifferenceData> GidsArray, int semanticLinkId, int tripid)
        {
            int segmentId=0;

            ////近傍リンクの絞込
            //string query = "START_LAT > " + (gids.latitude - 0.05);
            //query += " AND START_LAT < " + (gids.latitude + 0.05);
            //query += " AND START_LONG > " + (gids.longitude - 0.05);
            //query += " AND START_LONG < " + (gids.longitude + 0.05);
            //DataRow[] dataRows = dt.Select(query);
            //int startNum = 0;
            ////リンクデータを往路復路で分岐させてソート
            //if(semanticLinkId == 220 || semanticLinkId == 224)
            //{
            //    startNum = 192180;
            //}
            //else if(semanticLinkId == 221 || semanticLinkId == 225)
            //{
            //    startNum = 1591573;
            //}
            //DataTable LinkTable = LinkDateGetter.LinkTableGetter2(semanticLinkId);
            //LinkTable.DefaultView.Sort = "NUM";
            //DataRow[] LinkRows = LinkTable.Select(null, "NUM");
            //DataRow[] StartLink = LinkTable.Select("NUM = " + startNum);
            //List<LinkData> linkList = new List<LinkData>();

            //linkList.Add(new LinkData(Convert.ToString(StartLink[0]["LINK_ID"]), Convert.ToInt32(StartLink[0]["NUM"]),
            //    Convert.ToDouble(StartLink[0]["START_LAT"]), Convert.ToDouble(StartLink[0]["START_LONG"]),
            //    Convert.ToDouble(StartLink[0]["END_LAT"]), Convert.ToDouble(StartLink[0]["END_LONG"]), Convert.ToDouble(StartLink[0]["DISTANCE"])));
            ////スタート地点のリンクを初期値に設定

            //Boolean flag = true;
            //int j = 0;
            //while (flag)
            //{
            //    flag = false;
            //    for (int i = 0; i < LinkRows.Length; i++)
            //    {
            //        if (Convert.ToDouble(LinkRows[i]["START_LAT"]) == linkList[j].END_LAT && Convert.ToDouble(LinkRows[i]["START_LONG"]) == linkList[j].END_LONG
            //            && (Convert.ToDouble(LinkRows[i]["END_LAT"]) != linkList[j].START_LAT || Convert.ToDouble(LinkRows[i]["END_LONG"]) != linkList[j].START_LONG))
            //        {

            //            linkList.Add(new LinkData(Convert.ToString(LinkRows[i]["LINK_ID"]), Convert.ToInt32(LinkRows[i]["NUM"]),
            //    Convert.ToDouble(LinkRows[i]["START_LAT"]), Convert.ToDouble(LinkRows[i]["START_LONG"]),
            //    Convert.ToDouble(LinkRows[i]["END_LAT"]), Convert.ToDouble(LinkRows[i]["END_LONG"]), Convert.ToDouble(LinkRows[i]["DISTANCE"])));
            //            j++;
            //            flag = true;
            //            break;
            //        }
            //    }

            //}

            double minDist = 255;


            int tempNum = 0;
            string tempLinkId = null;
            double offset = 0;
            //各リンクセグメントに対して
            for (int i = 0; i < linkList.Count; i++)
            {
                Vector2D linkStartEdge = new Vector2D(linkList[i].START_LAT, linkList[i].START_LONG);
                Vector2D linkEndEdge = new Vector2D(linkList[i].END_LAT, linkList[i].END_LONG);
                Vector2D GPSPoint = new Vector2D(gids.latitude, gids.longitude);

                //線分内の最近傍点を探す
                Vector2D matchedPoint = Vector2D.nearest(linkStartEdge, linkEndEdge, GPSPoint);

                //最近傍点との距離
                double tempDist = Vector2D.distance(GPSPoint, matchedPoint);


                //リンク集合の中での距離最小を探す
                if (tempDist < minDist)
                {
                    GeoCoordinate linkStart = new GeoCoordinate();
                    linkStart.Latitude = linkList[i].START_LAT;
                    linkStart.Longitude = linkList[i].START_LONG;
                    GeoCoordinate gpsPoint = new GeoCoordinate();
                    gpsPoint.Latitude = gids.latitude;
                    gpsPoint.Longitude = gids.longitude;

                        minDist = tempDist;

                    tempNum = linkList[i].NUM;
                    tempLinkId = linkList[i].LINK_ID;

                    offset = HubenyDistanceCalculator.CalcHubenyFormula(linkStart, gpsPoint);
                }
            }
            //セグメントIDの決定
            DataTable linkListTable = LinkDateGetter.LinkListGetter(semanticLinkId);
            DataTable segmentTable = LinkDateGetter.SegmentGetter(semanticLinkId);

           // segmentId = searchSegmentId(linkListTable, segmentTable, linkList, tempNum, tempLinkId, offset);

            GidsDifferenceData resultGids = new GidsDifferenceData(segmentId, semanticLinkId, tripid, Convert.ToInt32(gids.deltaGids), Convert.ToDateTime(gids.jst));
            GidsArray.Add(resultGids);

            return minDist;
        }
        double searchNearestLinkforECOLOG(List<LinkData> linkList, ECOLOGData ecolog, List<ConsumedElectricEnergyData> ECOLOGArray, int semanticLinkId, int tripid)
        {
            int segmentId = 0;

            ////近傍リンクの絞込
            //string query = "START_LAT > " + (ecolog.latitude - 0.05);
            //query += " AND START_LAT < " + (ecolog.latitude + 0.05);
            //query += " AND START_LONG > " + (ecolog.longitude - 0.05);
            //query += " AND START_LONG < " + (ecolog.longitude + 0.05);
            //DataRow[] dataRows = dt.Select(query);
            //int startNum = 0;
            ////リンクデータを往路復路で分岐させてソート
            //if (semanticLinkId == 220 || semanticLinkId == 224)
            //{
            //    startNum = 192180;
            //}
            //else if (semanticLinkId == 221 || semanticLinkId == 225)
            //{
            //    startNum = 1591573;
            //}
            //DataTable LinkTable = LinkDateGetter.LinkTableGetter2(semanticLinkId);
            //LinkTable.DefaultView.Sort = "NUM";
            //DataRow[] LinkRows = LinkTable.Select(null, "NUM");
            //DataRow[] StartLink = LinkTable.Select("NUM = " + startNum);
            //List<LinkData> linkList = new List<LinkData>();

            //linkList.Add(new LinkData(Convert.ToString(StartLink[0]["LINK_ID"]), Convert.ToInt32(StartLink[0]["NUM"]),
            //    Convert.ToDouble(StartLink[0]["START_LAT"]), Convert.ToDouble(StartLink[0]["START_LONG"]),
            //    Convert.ToDouble(StartLink[0]["END_LAT"]), Convert.ToDouble(StartLink[0]["END_LONG"]), Convert.ToDouble(StartLink[0]["DISTANCE"])));
            ////スタート地点のリンクを初期値に設定

            //Boolean flag = true;
            //int j = 0;
            //while (flag)
            //{
            //    flag = false;
            //    for (int i = 0; i < LinkRows.Length; i++)
            //    {
            //        if (Convert.ToDouble(LinkRows[i]["START_LAT"]) == linkList[j].END_LAT && Convert.ToDouble(LinkRows[i]["START_LONG"]) == linkList[j].END_LONG
            //            && (Convert.ToDouble(LinkRows[i]["END_LAT"]) != linkList[j].START_LAT || Convert.ToDouble(LinkRows[i]["END_LONG"]) != linkList[j].START_LONG))
            //        {

            //            linkList.Add(new LinkData(Convert.ToString(LinkRows[i]["LINK_ID"]), Convert.ToInt32(LinkRows[i]["NUM"]),
            //    Convert.ToDouble(LinkRows[i]["START_LAT"]), Convert.ToDouble(LinkRows[i]["START_LONG"]),
            //    Convert.ToDouble(LinkRows[i]["END_LAT"]), Convert.ToDouble(LinkRows[i]["END_LONG"]), Convert.ToDouble(LinkRows[i]["DISTANCE"])));
            //            j++;
            //            flag = true;
            //            break;
            //        }
            //    }

            //}

            double minDist = 255;


            int tempNum = 0;
            string tempLinkId = null;
            double offset = 0;
            //各リンクセグメントに対して
            for (int i = 0; i < linkList.Count; i++)
            {
                Vector2D linkStartEdge = new Vector2D(linkList[i].START_LAT, linkList[i].START_LONG);
                Vector2D linkEndEdge = new Vector2D(linkList[i].END_LAT, linkList[i].END_LONG);
                Vector2D GPSPoint = new Vector2D(ecolog.latitude, ecolog.longitude);

                //線分内の最近傍点を探す
                Vector2D matchedPoint = Vector2D.nearest(linkStartEdge, linkEndEdge, GPSPoint);

                //最近傍点との距離
                double tempDist = Vector2D.distance(GPSPoint, matchedPoint);


                //リンク集合の中での距離最小を探す
                if (tempDist < minDist)
                {
                    GeoCoordinate linkStart = new GeoCoordinate();
                    linkStart.Latitude = linkList[i].START_LAT;
                    linkStart.Longitude = linkList[i].START_LONG;
                    GeoCoordinate gpsPoint = new GeoCoordinate();
                    gpsPoint.Latitude = ecolog.latitude;
                    gpsPoint.Longitude = ecolog.longitude;

                    minDist = tempDist;

                    tempNum = linkList[i].NUM;
                    tempLinkId = linkList[i].LINK_ID;

                    offset = HubenyDistanceCalculator.CalcHubenyFormula(linkStart, gpsPoint);
                }
            }
            //セグメントIDの決定
            DataTable linkListTable = LinkDateGetter.LinkListGetter(semanticLinkId);
            DataTable segmentTable = LinkDateGetter.SegmentGetter(semanticLinkId);
            segmentTable.DefaultView.Sort = "START_LINK_ID,START_NUM";

            segmentId = searchSegmentId(linkListTable, segmentTable, linkList, tempNum, tempLinkId, offset);

            ConsumedElectricEnergyData result = new ConsumedElectricEnergyData(segmentId, semanticLinkId, tripid, Convert.ToDouble(ecolog.consumedElectricEnergy), Convert.ToDateTime(ecolog.jst));
            ECOLOGArray.Add(result);

            return minDist;
        }

        int searchSegmentId(DataTable linkListTable, DataTable segmentTable, List<LinkData> linkList, int linkNum, string linkId, double coordinateOffset)
        {
            int element = -1000;
            int segmentId = 0;
            string query = "START_LINK_ID = '" + linkId + "'";
            DataRow[] segmentRow = segmentTable.Select(query);
            DataRow[] segmentRowBefore = new DataRow[1];

            if (segmentRow.Length >= 1)//リンクに対してセグメントが複数の場合
            {
                query = "START_LINK_ID = '" + linkId + "' AND START_NUM =" + linkNum;
                segmentRow = segmentTable.Select(query);
                if(segmentRow.Length == 0)
                {
                    element = getBeforeElement(linkList, linkNum, linkId, segmentTable);
                    query = "START_LINK_ID = '" + linkList[element].LINK_ID + "' AND START_NUM = " + linkList[element].NUM;
                    segmentRowBefore = segmentTable.Select(query, "SEGMENT_ID");
                    segmentId = (int)segmentRowBefore[segmentRowBefore.Length - 1]["SEGMENT_ID"];
                    if (segmentId == 0)
                    {
                        errorLabel.Text = "generatesegment0";
                        errorLabel.Update();
                    }
                    return segmentId;
                }
                 else if ((double)segmentRow[0]["START_POINT_OFFSET"] != 0) //オフセットがゼロではないの場合
                    {
                        element = getBeforeElement(linkList, linkNum, linkId, segmentTable);
                        query = "START_LINK_ID = '" + linkList[element].LINK_ID + "' AND START_NUM = " + linkList[element].NUM;
                        segmentRowBefore = segmentTable.Select(query, "SEGMENT_ID");

                    }

                    if (coordinateOffset < (double)segmentRow[0]["START_POINT_OFFSET"])//最初のセグメントがリンクをまたがっている場合
                    {
                        segmentId = (int)segmentRowBefore[segmentRowBefore.Length - 1]["SEGMENT_ID"];
                    if (segmentId == 0)
                    {
                        errorLabel.Text = "generatesegment0";
                        errorLabel.Update();
                    }
                    return segmentId;
                    }
                    
                

                int i = 0;
                while (true)
                {
                    i++;
                    if(i == segmentRow.Length)
                    {
                        segmentId = (int)segmentRow[segmentRow.Length - 1]["SEGMENT_ID"];
                        break;
                    }


                    if((double)segmentRow[i]["START_POINT_OFFSET"] > coordinateOffset )
                    {
                        segmentId = (int)segmentRow[i-1]["SEGMENT_ID"];
                        break;
                    }
                }
             }
            else if (segmentRow.Length == 0)
            {
                element = getBeforeElement(linkList, linkNum, linkId, segmentTable);
                query = "START_LINK_ID = '" + linkList[element].LINK_ID + "' AND START_NUM = " + linkList[element].NUM;
                segmentRowBefore = segmentTable.Select(query, "SEGMENT_ID");
                segmentId = (int)segmentRowBefore[segmentRowBefore.Length - 1]["SEGMENT_ID"];
                if (segmentId == 0)
                {
                    errorLabel.Text = "generatesegment0";
                    errorLabel.Update();
                }
                return segmentId;
            }
            if(segmentId == 0)
            {
                errorLabel.Text = "generatesegment0";
                errorLabel.Update();
            }
            return segmentId;
        }

        int getBeforeElement(List<LinkData> linkList, int linkNum, string linkId, DataTable segmentTable)
        {
            int element = -1000;
            int tempNum = linkNum;
            string tempLinkId = linkId;
            while (true)
            {
                for (int i = 0; i < linkList.Count; i++)
                {

                    if (linkList[i].NUM == tempNum && linkList[i].LINK_ID == tempLinkId)
                    {
                        element = i - 1;
                        tempNum = linkList[element].NUM;
                        tempLinkId = linkList[element].LINK_ID;
                        break;
                    }
                }
                DataRow[] segmentRow = segmentTable.Select("START_LINK_ID = '" + tempLinkId + "' AND START_NUM = " + tempNum);
                if (segmentRow.Length >= 1)
                {
                    break;
                }

            }
            return element;
        }

        public int getMinElement(double[] colle)
        {
            int temp = 0;

            for (int i = 1; i < colle.Length; i++)
            {
                if (colle[temp] > colle[i])
                    temp = i;
            }
            return temp;
        }
    }
    class GPSData
    {
        public DateTime GPSTime { get; set; }
        public DateTime androidTime { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double altitude { get; set; }
        public double accuracy { get; set; }

        public GPSData(DateTime time1, DateTime time2, double la, double lo, double al, double ac)
        {
            GPSTime = time1;
            androidTime = time2;
            latitude = la;
            longitude = lo;
            altitude = al;
            accuracy = ac;
        }
    }
    class GidsData
    {
        public int tripId { get; set; }
        public DateTime jst { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double deltaGids { get; set; }

        public GidsData(int tripId, DateTime jst, double latitude, double longitude, double deltaGids)
        {
            this.tripId = tripId;
            this.jst = jst;
            this.latitude = latitude;
            this.longitude = longitude;
            this.deltaGids = deltaGids;
        }
    }
    class ECOLOGData
    {
        public int tripId { get; set; }
        public DateTime jst { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double consumedElectricEnergy { get; set; }

        public ECOLOGData(int tripId, DateTime jst, double latitude, double longitude, double consumedElectricEnergy)
        {
            this.tripId = tripId;
            this.jst = jst;
            this.latitude = latitude;
            this.longitude = longitude;
            this.consumedElectricEnergy = consumedElectricEnergy;
        }
    }
    public class GidsDifferenceData
    {
        public int segmentId { get; set; }
        public int semanticLinkId { get; set; }
        public int tripId { get; set; }
        public int gidsDifference { get; set; }
        public DateTime jst { get; set; }
        public GidsDifferenceData(int segmentId, int semanticLinkId, int tripId, int gidsDifference,DateTime jst)
        {
            this.semanticLinkId = semanticLinkId;
            this.segmentId = segmentId;
            this.tripId = tripId;
            this.gidsDifference = gidsDifference;
            this.jst = jst;
        }
    }
    public class ConsumedElectricEnergyData
    {
        public int segmentId { get; set; }
        public int semanticLinkId { get; set; }
        public int tripId { get; set; }
        public double consumedElectricEnergy { get; set; }
        public DateTime jst { get; set; }
        public ConsumedElectricEnergyData(int segmentId, int semanticLinkId, int tripId, double consumedElectricEnergy, DateTime jst)
        {
            this.semanticLinkId = semanticLinkId;
            this.segmentId = segmentId;
            this.tripId = tripId;
            this.consumedElectricEnergy = consumedElectricEnergy;
            this.jst = jst;
        }
    }
    class Vector2D
    {
        public double x { get; set; }
        public double y { get; set; }

        public Vector2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        //点Pから線分ABへの最も近い点を探索する
        public static Vector2D nearest(Vector2D A, Vector2D B, Vector2D P)
        {
            Vector2D a = new Vector2D(B.x - A.x, B.y - A.y);
            Vector2D b = new Vector2D(P.x - A.x, P.y - A.y);
            double r = (a.x * b.x + a.y * b.y) / (a.x * a.x + a.y * a.y);

            if (r <= 0)
            {
                return A;
            }
            else if (r >= 1)
            {
                return B;
            }
            else
            {
                Vector2D result = new Vector2D(A.x + r * a.x, A.y + r * a.y);
                return result;
            }
        }

        //線分ABの長さ
        public static double distance(Vector2D A, Vector2D B)
        {
            return Math.Sqrt((A.x - B.x) * (A.x - B.x) + (A.y - B.y) * (A.y - B.y));
        }
    }
    class LinkData
    {
        public string LINK_ID { get; set; }
        public int NUM { get; set; }
        public double START_LAT { get; set; }
        public double START_LONG { get; set; }
        public double END_LAT { get; set; }
        public double END_LONG { get; set; }

        public double DISTANCE { get; set; }

        public LinkData(String LINK_ID, int NUM, double START_LAT, double START_LONG, double END_LAT, double END_LONG, double DISTANCE)
        {
            this.LINK_ID = LINK_ID;
            this.NUM = NUM;
            this.START_LAT = START_LAT;
            this.START_LONG = START_LONG;
            this.END_LAT = END_LAT;
            this.END_LONG = END_LONG;
            this.DISTANCE = DISTANCE;
        }
    }
}
