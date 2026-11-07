using System.Collections.Generic;
using CityScooter.Rendering;
using UnityEngine;

namespace CityScooter.EditorTools.Art
{
    internal static class ComicCityDefaults
    {
        public static void Populate(ComicCityRecipe r)
        {
            var props=new List<ComicAssetSpec>();
            void P(string id,string label,string style,float w,float h,float d,Color color)=>props.Add(new ComicAssetSpec{id=id,label=label,style=style,width=w,height=h,depth=d,color=color});
            P("Hydrant","消防栓","hydrant",.5f,1,.5f,r.red);
            P("Bin","圆筒垃圾桶","bin",.65f,1,.65f,r.green);
            P("RecycleBins","分类垃圾箱","recycle",1.5f,1,.75f,r.glass);
            P("NewspaperBox","报刊箱","newspaper",.65f,1.2f,.5f,r.yellow);
            P("Mailbox","邮筒","mail",.6f,1.3f,.6f,r.red);
            P("ParcelLocker","快递柜","locker",2,2,.6f,r.yellow);
            P("UtilityBox","配电箱","utility",1,1.5f,.5f,r.green);
            P("AirConditioner","空调外机","ac",1,.65f,.5f,r.cream);
            P("TreePit","方形树池","tree",2,5,2,r.pavement);
            P("Planter","长条花箱","planter",2,1,.8f,r.pavement);
            P("FlowerPot","圆形花盆","pot",.8f,1.2f,.8f,new Color(.8f,.39f,.24f));
            P("StonePost","石质矮墩","stone",.5f,.8f,.5f,r.pavement);
            P("Bollard","隔离柱","bollard",.4f,1,.4f,r.ink);
            P("Rail","短段护栏","rail",2,1,.3f,r.glass);
            P("StreetLamp","路灯","lamp",.8f,5,1,r.ink);
            P("StreetSign","路牌杆","sign",1.2f,3,.25f,r.green);
            P("ParkingMeter","停车计费器","meter",.35f,1.5f,.3f,r.glass);
            P("Bench","长椅","bench",2,1,.8f,new Color(.72f,.39f,.2f));
            P("BikeRack","自行车停车架","bike",2,1,.8f,r.glass);
            P("ShopBoard","店铺立牌","board",.75f,1.1f,.6f,r.yellow);
            P("VendingMachine","自动售货机","vending",1,2,.7f,r.red);
            P("Crates","货箱组","crates",1,1,.8f,new Color(.65f,.42f,.23f));
            P("Cones","交通锥组","cones",1,.75f,.5f,r.red);
            P("WorkBarrier","施工挡板","barrier",1.8f,1,.65f,r.yellow);
            P("ConcreteBarrier","水泥隔离墩","concrete",2,1,.75f,r.pavement);
            P("BusShelter","公交候车亭","shelter",4,3,1.5f,r.glass);
            r.props=props.ToArray();
            var buildings=new List<ComicAssetSpec>();
            Color coral=new Color(.86f,.43f,.31f),teal=new Color(.28f,.59f,.59f),ochre=new Color(.88f,.65f,.32f),purple=new Color(.57f,.49f,.64f),stone=new Color(.72f,.74f,.67f);
            void B(string id,string label,string style,int floors,float w,float d,Color color)=>buildings.Add(new ComicAssetSpec{id=id,label=label,style=style,width=w,height=floors*r.floorHeight,depth=d,floors=floors,color=color});
            B("CornerStore","街角便利店","corner",3,8,12,coral);
            B("Cafe","咖啡店","cafe",2,8,12,teal);
            B("Bakery","面包店","shop",3,8,12,ochre);
            B("PizzaShop","披萨店","shop",3,8,12,coral);
            B("Diner","小餐馆","cafe",2,8,12,purple);
            B("Barber","理发店","shop",3,4,8,teal);
            B("Laundry","洗衣店","shop",3,8,12,stone);
            B("Bookshop","书店","shop",3,8,12,purple);
            B("ScooterShop","滑板车店","shop",3,8,12,teal);
            B("Pharmacy","药店","civic",3,8,12,stone);
            B("Hardware","五金店","garage",2,8,12,ochre);
            B("Garage","汽车维修店","garage",1,8,12,teal);
            B("Market","小型超市","shop",2,12,12,ochre);
            B("PostOffice","邮局","civic",3,8,12,coral);
            B("Bank","银行","bank",4,12,12,stone);
            B("Clinic","社区诊所","civic",3,8,12,teal);
            B("Cinema","电影院","cinema",3,12,12,purple);
            B("RowHouse","联排住宅","residential",3,4,12,coral);
            B("ShopApartment","商住公寓","shop",5,8,12,ochre);
            B("CornerApartment","转角公寓","corner",5,8,16,coral);
            B("Office","办公楼","office",7,8,16,teal);
            B("Hotel","街区旅馆","hotel",6,8,16,purple);
            B("Warehouse","仓库","warehouse",2,12,16,stone);
            B("ParkingGarage","停车楼","parking",4,12,16,stone);
            B("Tower","城市塔楼","tower",12,12,16,teal);
            B("BackgroundBlock","背景楼群模块","background",8,12,16,purple);
            r.buildings=buildings.ToArray();
        }
    }
}
