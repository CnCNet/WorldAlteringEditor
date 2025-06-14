using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Models.ArtConfig;
using TSMapEditor.Rendering;
using TSMapEditor.GameMath;
using TSMapEditor.Rendering.ObjectRenderers;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.Windows;
using TSMapEditor.Misc;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Models.Enums;
using TSMapEditor.Translations;
using TSMapEditor.Settings;

namespace TSMapEditor.UI.Sidebar
{
    /// <summary>
    /// A base class for all object type list panels.
    /// </summary>
    public abstract class ObjectListPanel : XNAPanel, ISearchBoxContainer
    {
        /// <summary>
        /// Helper structure used for building sidebar object categories.
        /// Not used after the sidebar has been initialized.
        /// </summary>
        struct ObjectCategory
        {
            public string Name;
            public Color RemapColor;

            public ObjectCategory(string name, Color remapColor)
            {
                Name = name;
                RemapColor = remapColor;
            }
        }

        private static readonly Dictionary<string, (string zh, string en)> CategoryNameMap = new Dictionary<string, (string zh, string en)>
        {
            { "Allies", ("同盟国联军", "Allies") },
            { "Pacific", ("太平洋阵线", "Pacific") },
            { "Soviets", ("苏维埃联盟", "Soviets") },
            { "USSR", ("苏维埃俄罗斯", "USSR") },
            { "Latin", ("拉丁同盟", "Latin") },
            { "PsiCorps", ("心灵军团", "PsiCorps") },
            { "ScorpionCell", ("天蝎组织", "ScorpionCell") },
            { "Headquarters", ("总部守卫", "Headquarters") },
            { "Guild1", ("狂鲨先锋", "Guild1") },
            { "Guild2", ("科洛尼亚侧翼", "Guild2") },
            { "Guild3", ("最后堡垒", "Guild3") },
            { "Europeans", ("欧洲联盟", "Europeans") },
            { "UnitedStates", ("美国", "UnitedStates") },
            { "Chinese", ("中国", "Chinese") },
            { "Civilian", ("平民", "Civilian") },
            { "Uncategorized", ("未分类", "Uncategorized") },
            { "Yuri", ("厄普西隆", "Yuri") },
            { "Tech", ("科技建筑", "Tech") },
            { "Neutral", ("中立", "Neutral") },
            { "Special", ("特殊", "Special") },
            { "Headquaters", ("总部守卫", "Headquaters") },
           
            
            
        };

        // 对象名汉化映射表（ININame为key，中文名为value）
        private static readonly Dictionary<string, string> TechnoChineseNameMap = new Dictionary<string, string>
        {
            { "AMCV", "盟军机动建设车" },
            { "CMIN", "超时空采矿车" },
            { "UMIN", "极限采矿车" },
            { "ROBO", "机器人坦克" },
            { "FV", "斯特瑞克步兵战车" },
            { "ETNK", "斗牛犬轻型坦克" },
            { "AMC", "执政官装甲步兵车" },
            { "MTNK", "骑士中型坦克" },
            { "TENGU", "长剑步兵装甲" },
            { "KTNK", "河童悬浮坦克" },
            { "AHMV", "武装悍马车" },
            { "AERO", "天火防空坦克" },
           
            { "BASS", "雅典娜炮" },
            { "ABRM", "艾布拉姆斯坦克" },
            { "MGTK", "幻影坦克" },
            { "CHRTNK", "冥卫坦克" },
            { "SREF", "光棱坦克" },
            { "HOWI", "西风火炮" },
            { "BLZZ", "暴风雪坦克" },
            { "BFRT", "玄武战斗要塞" },
            { "VCARR", "冰雹平台" },
            { "QUICK", "跃迁载具" },
            { "CHRP", "超时空监狱" },
            { "AHVYBOT2", "未来坦克阿尔法" },
            { "STORM", "风暴之子战机" },
            { "ORCA", "鹞式战机" },
            { "BEAG", "黑鹰战机" },
            { "HBIRD", "蜂鸟支援机" },
            { "FORTRESS", "梭鱼轰炸机" },
            { "SHAD", "千里马运输直升机" },
            { "CRYO", "冷冻直升机" },
            { "COMA", "战鹰直升机" },
            { "THOR", "雷神炮艇" },
            { "LIONH", "狮心王轰炸机" },
            { "LCRF", "旅行者运输艇" },
            { "DEST", "驱逐舰" },
            { "AEGIS", "神盾巡洋舰" },
            { "DLPH", "海豚" },
	        { "AMEDIC", "军医" },
            { "ARMR", "友川纪夫" },
            { "ASSN", "拉恩" },
            { "CLEG", "超时空军团兵" },
            { "E1", "盟军大兵" },
            { "ENFO", "光棱攻城兵" },
            { "GGI", "守卫大兵" },
            { "GHOST", "海豹突击队" },
            { "JUMPJET", "火箭飞行兵" },
            { "RIOT", "镇暴部队" },
            { "SIEG", "西格弗里德" },
            { "SNIPE", "狙击手" },
            { "SUPR", "反转士" },
            { "TANY", "谭雅" },
            { "ADOG", "盟军军犬" },
            { "AIMORA", "莫拉莱斯(AI)" },
            { "ARSO", "纵火狂" },
            { "CHITZ", "契特卡伊" },
            { "DESOR", "辐射根除者" },
            { "E2", "动员兵" },
            { "FLAMER", "喷火工兵" },
            { "GYRO", "旋翼飞行兵" },
            { "IVAN", "疯狂伊文" },
            { "MORALES", "莫拉莱斯" },
            { "MOTOR", "火炮机车" },
            { "SHK", "磁暴步兵" },
            { "SHOCK", "闪流步兵" },
            { "SBTR", "破坏者" },
           
            { "VOLKOV", "沃尔科夫" },
            { "YUNRU", "云茹" },
            { "SENGINEER", "苏联工程师" },
            { "DUNE", "沙丘骑兵" },
            { "HARP", "弓箭手" },
            { "INIT", "尤里新兵" },
            { "INTRUDER", "渗透者" },
            { "KAOS", "毒爆虱" },
            { "LIBRA", "天秤" },
            { "REPU", "磁震恶徒" },
            { "SCRG", "天灾突袭兵" },
            { "STALKER", "梦魇行者" },
            { "UNDER", "马尔翁" },
            { "VIRUS", "病毒狙击手" },
            { "YENGINEER", "厄普西隆工程师" },
            { "PROS", "异教" },
            { "CYBO", "半机械人先驱" },
            { "KRUKOV", "库可夫" },
            { "REZNOV", "雷泽诺夫" },
            { "D_COVE", "长枪战士(假)" },
            { "D_KNIGHT", "铁骑兵(假)" },
            { "CLAIRAI", "千里眼机器人(AI)" },
            { "D_CLAIR", "千里眼机器人(假)" },
            { "AITHOR", "雷神炮艇(AI)" },
            { "AIHOWI", "西风火炮(AI)" },
            { "CRYOAI", "冷冻直升机(AI)" },
            { "SAPC", "野牛运输艇" },
            { "UMINW", "极限采矿车(水上)" },
            { "SUNB", "猎杀机器人" },
            { "SUNB_1", "猎杀机器人载员" },
            { "CYCL_1", "独眼装甲巨人近身载员" },
            { "DRON2", "恐怖机器人(非主动)" },
            { "BURA2", "布拉提诺火箭车(散射)" },
            { "ARMAW", "犰狳重型载具(水上)" },
            { "FDRON", "怒焰地雷机器人" },
            { "EDRN", "蜻蜓无人机" },
            { "GCHF", "工厂停产维护触发者" },
            { "REAP", "收割巡逻艇" },
            { "REAPL", "收割巡逻艇(陆上)" },
            { "HCAN", "九头蛇炮" },
            { "RAVA", "德拉库夫机动监狱" },
            { "SCHP", "劫掠者武装直升机" },
            { "SCHPAI", "劫掠者武装直升机(AI)" },
            { "TNKKIL", "坦克歼击车" },
            { "ZEP", "基洛夫飞艇" },
            { "WOLF", "猎狼直升机" },
            { "BOREKW", "破坏神载具(水上)" },
            { "NCHF", "纳米回复场触发者" },
            { "TRIKE2", "极速三轮摩托车(散射)" },
            { "AHVYBOT", "未来坦克X-0" },
            { "ARCHW", "恐龟加农炮(水上)" },
            { "ARTY", "老式粉碎火炮" },
            { "D_QUETZ", "风神翼龙(假)" },
            { "CONFD", "激怒者支援车(部署)" },
            { "D_VIPER", "无齿翼龙攻击机" },
            { "DRACOD", "飞蜥无人机" },
            { "F_KSNAK", "王蛇战机载点" },
            { "GHTNKW", "巨鳄载具(水上)" },
            { "MSAW", "机动声波探测仪(水上)" },
            { "ROADR2", "走鹃活动炸弹(非主动)" },
            { "VENTREX", "冰雹攻击机" },
            { "RECON", "侦察无人机" },
            { "MIGSPY", "维修无人机投放飞机" },
            { "HORNET", "黄蜂无人机" },
            { "DUST", "尘旋风支援机" },
            { "ASW", "鱼鹰无人机" },
            { "SNAKE", "王蛇支援战机" },
            { "B52", "B52轰炸机" },
            { "DMISL", "无畏舰导弹" },
            { "FOX", "狐步舞者战机" },
            { "ICBMROCKET", "白杨M导弹" },
            { "TU16", "烟雾弹投放飞机" },
            { "V2ROCKET", "V2导弹" },
            { "V3ROCKET", "飞毛腿导弹" },
            { "WASTE", "自爆无人机" },
            { "BPLNE", "精英米格X原型机" },
            { "HARB", "先锋炮艇机" },
            { "IRONWING", "铁翼" },
            { "WIDOWA", "黑寡妇阿尔法支援机" },
            { "WIDOW", "黑寡妇支援机" },
            { "STARDUST", "悖论引擎(AI)" },
            { "BPLN", "米格X原型机" },
            { "SPYP", "侦察机" },
            { "CMISL", "阿库拉导弹" },
            { "D_SEEKERD", "风神翼龙无人机(假)" },
            { "SHRIKE", "伯劳无人机" },
            { "SEEKERD", "风神翼龙无人机" },
            { "LEVIA", "利维坦等离子无人轰炸机" },
            { "DBOAT", "烈蚊自爆快艇" },
            { "DRED", "无畏级导弹舰" },
            { "SUB2", "台风攻击潜艇(电磁炮)" },
            { "SWLF", "海狼" },
            { "ARCH2", "恐龟加农炮(非主动)" },
            { "LONGBO", "老式长弓直升机" },
            { "MECHA", "天狗机甲" },
            { "PERUN", "佩龙旗舰" },
            { "SDRN", "维修无人机" },
            { "STARDUSTB", "悖论引擎" },



            { "ROBOW", "机器人坦克(水上)" },
            { "SIREN", "塞壬护卫舰" },
            { "CARRIER", "航空母舰" },
            { "HCRUIS", "战列舰" },
            { "GACNST", "盟军建造场" },
            { "GAPOWR", "发电厂" },
            { "GAPOWRUP", "电力涡轮" },
            { "GAPILE", "盟军兵营" },
            { "GAREFN", "盟军矿石精炼厂" },
            { "GAWEAP", "盟军战争工厂" },
            { "GADEPO", "盟军军械库" },
            { "GAYARD", "盟军海军船坞" },
            { "GAAIRC", "盟军空军指挥部" },
            { "GAAIRC_D", "空降控制站" },
            { "GAMERC", "墨丘利卫星系统终端" },
            { "GASCEA", "防御指挥部" },
            { "GASCPF", "机器人控制中心" },
            { "GATECH", "科技中心" },
            { "GACLAB", "实验型传送平台" },
            { "GAOREP", "矿石提纯器" },
            { "GACSPH", "超时空传送仪" },
            { "GAWEAT", "天气控制仪" },
            { "GAWALL", "盟军混凝土墙" },
            { "GAGATE_D", "盟军闸门(东西)" },
            { "GAGATE_C", "盟军闸门(南北)" },
            
            { "GAPILL", "机枪碉堡" },
            { "GACPIL", "迷彩机枪碉堡" },
            { "GAGUN", "定点炮台" },
            { "NASAM", "爱国者导弹" },
            { "ATESLA", "光棱塔" },
            { "GTGCAN", "巨炮" },
            { "GAPOST", "传送节点" },
            { "GAHPAD", "扩展跑道" },
            { "GAGAP", "裂缝制造仪" },
            { "GASTAS", "火控穹顶" },
            { "GACRYOS", "冰冻地雷" },
            { "GAHYPE", "休伯利安冷冻防空炮" },
            { "GASNPR", "盟军哨塔" },
            { "GABARR", "老式盟军兵营" },
            { "GAREF", "老式盟军矿石精炼厂" },
            { "GAPWR", "老式盟军发电厂" },
            { "GAAPP", "老式盟军进阶发电厂" },
            { "GAFACT", "老式盟军战车工厂" },
            { "GADOME", "老式盟军雷达球" },
            { "GAAAGUN", "老式防空炮" },
            { "GARGAP", "老式雷达干扰器" },
            { "GAMGG", "老式移动裂缝产生器(部署状态)" },
            { "GAPDOX", "老式超时空传送仪" },
            { "CADEFN", "短剑防御系统A" },
            { "CADEFNB", "短剑防御系统B" },
            { "CADEFNC", "短剑防御系统C" },
            { "CAFUSI", "地震平衡杆" },
            { "CACOOL01", "低温冷却液A" },
            { "CACOOL02", "低温冷却液B" },
            { "CACOOL03", "低温冷却液C" },
            { "CACOOL04", "低温冷却液D" },
            { "CACRTK01", "冷冻罐A" },
            { "CACRTK02", "冷冻罐B" },
            { "CACRTK03", "冷冻罐C" },
            { "CALAB", "命运科技实验室" },
           
            { "CASTOF01", "命运科技办公楼A" },
            { "CASTOF02", "命运科技办公楼B" },
            { "CASTOF03", "命运科技办公楼C" },
            { "CASTOF04", "命运科技办公楼D" },
            { "CASTOF05", "命运科技办公楼塔A" },
            { "CASTOF06", "命运科技办公楼塔B" },
            { "CAVHNG", "命运科技机库" },
            { "CABATT", "超载的悖论电池" },
            { "GACOND", "悖论传导器" },
            { "GAENGN", "悖论引擎残骸" },
            { "CAKANE05", "金川装配车间" },
            { "CAKANE01", "金川实验室A" },
            { "CAKANE02", "金川实验室B" },
            { "CAKANE04", "金川实验室C" },
            { "CAKANE06", "金川工业总部" },
            { "AICLEG", "超时空军团兵(AI)" },
            { "CMON", "超时空采矿车(空)" },
            { "UMON", "极限采矿车(空)" },
            { "PANTHER", "圣骑士歼击坦克" },
            { "ETNK2", "斗牛犬坦克(部署)" },
            { "AERO2", "天火坦克(远程)" },
            { "BASS2", "雅典娜炮(非主动)" },
            { "CHRTNK2", "冥卫坦克(非主动)" },
            { "SIREN2", "塞壬护卫舰(反转)" },
            { "AHVYBOT2B", "未来坦克α(部署)" },
            { "AMWC", "科技卡车A" },
            { "AMWB", "科技卡车B" },
            { "AMWD", "科技卡车C" },
            { "GABSTA", "断电武器研究站" },
            { "CARADI", "无线电塔A" },
            { "CARATO", "无线电塔B" },
            { "FTHQ", "命运科技塔" },
            { "GARAIN", "气象晶体" },
            { "CAWASH01", "白宫" },
            { "SMCV", "苏联机动建设车" },
            { "HARV", "武装采矿车" },
            { "HTK", "半履带车" },
            { "SCAR", "猛虎装甲运输车" },
            { "HTNK", "犀牛重型坦克" },
            { "JTNK", "捷豹突击坦克" },
            { "CTNK", "麒麟重型坦克" },
            { "DRON", "恐怖机器人" },
            { "BOREK", "破坏神载具" },
            { "ARMA", "犰狳重型载具" },
            { "DTRUCK", "自爆卡车" },
            { "BGGY", "自爆吉普车" },
            { "MWF", "斯大林之拳" },
            { "TTNK", "磁能巡航坦克" },
            { "V3", "飞毛腿导弹发射车" },
            { "APOC", "灾厄坦克" },
            { "BURA", "布拉提诺火箭炮车" },
            { "SENT", "哨兵防空车" },
            { "EMPR", "女娲加农炮" },
            { "CNTR", "百夫长攻城机甲" },
            { "GRUMBLE", "轰鸣防空导弹" },
            { "MAMM", "天启坦克" },
            { "YAK", "雅克战斗机" },
            { "MIG", "米格战斗机" },
            { "KAMOV", "卡莫夫直升机" },
            { "HIND", "米-24武装直升机" },
            { "KIROV", "基洛夫飞艇" },
            { "DREAD", "无畏级战列舰" },
            { "SUB", "攻击型潜艇" },
            { "TSUB", "台风级潜艇" },
            { "SSUB", "超级潜艇" },
            { "AKULA", "阿库拉潜艇" },
            { "SEAFL", "海豚级护卫舰" },
            { "SCOUT", "侦察兵" },
           
            { "FLAKT", "防空步兵" },
            { "TESLA", "磁暴线圈" },
            
            { "BRUTE", "野蛮人" },
            { "DOG", "战犬" },
            { "SPY", "间谍" },
            { "ENGINEER", "工程师" },
            { "DESO", "辐射工兵" },
            { "YURICL", "尤里复制人" },
            { "YURIM", "尤里突击队" },
            { "YURIPR", "尤里主脑" },
            { "YURITNK", "尤里坦克" },
            { "YURISUB", "尤里潜艇" },
            { "YURIDST", "尤里毁灭者" },
            { "YURICRU", "尤里巡洋舰" },
            { "PCV", "厄普西隆机动建设车" },
            { "YMIN", "幽灵采矿车" },
            { "STING", "毒刺无人载具" },
            { "YTNK", "加特林坦克" },
            { "LTNK", "鞭打者轻型坦克" },
            { "QTNK", "螳螂轻型坦克" },
            { "STNK", "奥普斯定制坦克" },
            { "DRIL", "钻地运输车" },
            { "MARA", "掠夺者坦克" },
            { "YAHCR", "炼狱防空平台" },
            { "MIND", "心灵之主" },
            { "TELE", "磁控坦克" },
            { "TRIKE", "极速三轮摩托车" },
            { "COYO", "氧化者防空支援车" },
            { "SCAV", "暴君" },
            { "PLAG", "瘟疫投石机" },
            { "SHADOW", "魔影坦克" },
            { "QUAD", "迷雾机动车" },
            { "DEVO", "巨像" },
            { "TRACTOR", "清道夫坦克" },
            { "WORMQ", "虫群女王" },
            { "BLIGHT", "恶灵战机" },
            { "VENOM", "恶灵基因升腾者" },
            { "DISK", "入侵飞碟" },
            { "BASIL", "毒蜥空中战舰" },
            { "GOTTER", "空中要塞伊利卡拉" },
            { "SEIZER", "恶灵摄魂师" },
            { "SALA", "火蜥蜴空中战舰" },
            { "YHVR", "神舟运输艇" },
            { "SLED", "食人鱼潜艇" },
            { "SQD", "巨型乌贼" },
            { "NAUT", "鹦鹉螺潜艇" },
            { "BSUB", "瘟神潜艇" },
            { "YACNST", "厄普西隆建造场" },
            { "YACXST", "假厄普西隆建造场" },
            { "YAPOWR", "生化反应室" },
            { "YABRCK", "厄普西隆兵营" },
            { "YARIREFN", "厄普西隆矿石精炼厂" },
            { "YAVULT", "厄普西隆合成缸" },
            { "YAWEAP", "厄普西隆战争工厂" },
            { "YAYARD", "厄普西隆海军船坞" },
            { "YAPSIS", "雷达旋塔" },
            { "YAAIRF", "停机坪" },
            { "YAGRND", "移动粉碎机" },
            { "YATECH", "潘多拉枢纽" },
            { "YAPVLT", "心灵研究组件" },
            { "YACVLT", "化学研究组件" },
            { "YAGVLT", "基因研究组件" },
            { "NACLON", "克隆缸" },
            { "YAGNTC", "狂暴发生器" },
            { "YAPPET", "心灵支配仪" },
            { "YAPPXT", "假心灵支配仪" },
            { "YBLABX", "假潘多拉枢纽" },
            { "GAFWLL", "护城墙" },
            { "YAGATE_B", "厄普西隆闸门(东西)" },
            { "YAGATE_A", "厄普西隆闸门(南北)" },
            { "YAGGUN", "加特林机炮" },
            { "NATBNK", "坦克碉堡" },
            { "YARAIL", "地狱热能塔" },
            { "YAMPSI", "心灵感应器" },
            { "YARIFT", "幻象核心" },
            { "YAHADE", "安瑞斯炮台" },
            { "YAPSYT", "心灵控制塔" },
            { "YAVNMMS", "基因地雷" },
            { "YAMREF", "雷格机动矿石精炼厂" },
            { "RavenSpecial", "侦查乌鸦" },
            { "QuickFortSpecial", "坦克碉堡崛起" },
            { "RegenDrugsSpecial", "恢复剂" },
            { "WonderDrugsSpecial", "治疗剂" },
            { "RuinerSpecial", "压制飞碟" },
            { "VisionSpecial", "心灵视界" },
            { "RisenMonolithSpecial", "速成方尖碑" },
            { "IllusionSpecial", "暗影环绕" },
            { "LibraCloneSpecial", "天秤复制人" },
            { "MagnetShiftSpecial", "磁力射线" },
            { "TickTrapSpecial", "毒爆虱伏击" },
            { "ToxicStrikeSpecial", "剧毒空袭" },
            { "HijackersSpecial", "空降劫持者" },
            { "KineticBarrierSpecial", "动能屏障" },
            { "MutationSpecial", "基因震爆" },
            { "PsychicFlashSpecial", "心灵之火" },
            { "RageInductorSpecial", "狂暴" },
            { "PsychicDominatorSpecial", "心灵支配" },
            { "RHAD", "拉什迪" },
            { "CBRIS", "太空特种兵" },
            { "SAVG", "蛮兽人" },
           
            { "HKAMAZ", "重型卡玛兹" },
            { "UTOPIA", "乌托邦运输仓" },
            { "ESDR", "监视者无人机" },
            { "CAOS", "基因突变车" },
            { "HEPH", "冥府守护神" },
            { "GRAV", "力神飞碟" },
            { "DHANDL", "左死神之手" },
            { "DHANDR", "右死神之手" },
            { "INFC", "感染者" },
            { "RADGUY", "黑曜石之海火焰人" },
            { "TBRUT", "转瞬即逝核爆狂兽人" },
            { "BRUTM", "心灵军团狂兽人" },
            { "BRUTV", "天蝎组织狂兽人" },
            { "BRUTS", "总部守卫狂兽人" },
            { "SCOMA", "梦魇特种兵" },
            { "LIBRAB", "天秤B" },
            { "LIBRAC", "天秤C" },
            { "QTNK2", "早期螳螂坦克" },
            { "STING2", "早期毒刺无人载具" },
            { "YAHCRWO", "月球炼狱平台" },
            { "TDISK", "病毒入侵飞碟" },
            { "NAPSYA", "心灵控制增幅器" },
            { "NAPSYD", "心灵增幅器残骸" },
            { "YAKAOS", "毒爆虱巢穴" },
            { "CAYURST", "尤里雕像" },
            { "CAEAST02", "尤里石像(挑战)" },
            { "YAROCK", "火箭发射台" },
            { "CAAZIZ", "拉什迪的宫殿" },
            { "YANEUR", "神经毒素缸" },
            { "YAHIVE", "恶灵防空巢" },
            { "YARCON", "火箭控制台" },
            { "YACOMD", "厄普西隆指挥中心" },
            { "YABIOB", "厄普西隆大型生化罐" },
            { "YABIO1", "厄普西隆生化罐A" },
            { "YABIO2", "厄普西隆生化罐B" },
            { "YABIO3", "厄普西隆生化罐C" },
            { "YASPAT", "天剑防御系统" },
            { "YASPOT", "厄普西隆哨塔" },
            { "YABOLT", "超能转换器" },
            { "YAPPPT", "心灵支配仪(建设中)" },
            { "YADYNA", "心灵能量源" },
            { "MODEV", "心灵终结仪" },
            { "YAOMGA", "主控旋塔" },
            { "YAPSYB", "厄普西隆心灵信标" },
            { "YAPROT", "厄普西隆建造厂原型" },
            { "YAMAGN", "磁控节点" },
            { "YAULAB", "地下设施实验室" },
            { "MOTHRA", "侦查乌鸦" },
            { "LIBRC", "天秤复制人" },
            { "HIJACKER", "劫持者" },
            { "YURIX", "尤里X" },
            { "SLAV", "奴隶" },
            { "XPCV", "假厄普西隆机动建设车" },
            { "YMON", "幽灵采矿车(空)" },
            { "SMIN", "雷格机动要塞" },
            { "GRND", "机动粉碎回收场" },
            { "MARAW", "掠夺者坦克(水上)" },
            { "COYOW", "氧化者防空支援车(水上)" },
            { "SHADOWF", "魔影坦克(非主动)" },
            { "DEVOD", "巨像(部署)" },
            { "MAGNET_1", "磁力射线载员" },
            { "MAGNET_2", "鹦鹉螺潜艇射线载员" },
            { "SALA_1", "火蜥蜴空中战舰混乱载员1" },
            { "SALA_2", "火蜥蜴空中战舰混乱载员2" },
            { "YAPLN", "恶灵拦截机" },
            { "YAPLNB", "恶灵巢拦截机" },
            { "DYBTR", "天秤克隆体投放飞机" },
            { "DBAT", "毒气投放飞机" },
            { "PSBOM", "瘟神导弹" },
            { "RUINER", "压制飞碟" },
            { "MAGNET", "磁力射线" },
            { "YABALL", "方尖碑" },
            { "NATBNKK", "应急坦克碉堡" },
            { "YAQUAD", "迷雾机动车(部署)" },
            { "YAVNMM", "基因地雷(衍生)" },
            { "YAVISN", "视界" },
            { "YABRCKB", "厄普西隆兵营(AI)" },
            { "YAWEAPB", "厄普西隆战车工厂(AI)" },
            { "YAYARDAI", "厄普西隆船坞(AI)" },
            { "YARIFTAI", "幻象核心(AI)" },
            { "YAPSYTAI", "心灵控制塔(AI)" },
            { "YAHADEAI", "安瑞斯炮台(AI)" },
            { "GenomineSpawn", "基因地雷投放" },
            { "FMCV", "焚风机动建设车" },
            { "NMIN", "矿甲虫" },
            { "JACKAL", "豺狼载具" },
            { "TERA", "雷鸟无人防空车" },
            { "CYCL", "独眼装甲巨人" },
            { "DRACO", "飞蜥轻型坦克" },
            { "ROACH", "蛮牛重型坦克" },
            { "MSA", "机动声波侦测仪" },
            { "RACC", "浣熊干扰车" },
            { "COON", "悬浮浣熊干扰车" },
            { "CONF", "激怒者支援车" },
            { "MEGA", "巨齿鲨机甲" },
            { "SHRAY", "沙特雷声波坦克" },
            { "MAD", "M.A.D.M.A.N." },
            { "ROADR", "走鹃活动炸弹" },
            { "TARCHIA", "多智龙火炮" },
            { "ORCIN", "虎鲸机动声雷达阵列" },
            { "SWPR", "扫荡者无人机" },
            { "PROME", "乳齿象坦克" },
            { "GHTNK", "巨鳄载具" },
            { "BOID", "机械造物" },
            { "PHNT", "幻光多管火箭炮" },
            { "SEITAAD", "塞泰龙弩炮" },
            { "ARCH", "恐龟加农炮" },
            { "RAMW", "公羊" },
            { "BUZZ", "秃鹰攻击机" },
            { "COND", "苍鹰攻击机" },
            { "DIVER", "爆裂蜂" },
            { "VIPER", "无齿翼龙攻击机" },
            { "HURR", "长生鸟空中哨站" },
            { "QUETZ", "风神翼龙" },
            { "SEAT", "海猫运输艇" },
            { "SWORD", "剑鱼护卫舰" },
            { "SHARK", "扁鲨迷幻潜艇" },
            { "MANTA", "魔鬼鱼无人防空战" },
            { "LEVI", "利维坦母舰" },
            { "FACNST", "焚风建造场" },
            { "FATRAP", "发电风箱" },
            { "FABARR", "焚风兵营" },
            { "FABARR_D", "网络流协议" },
            { "FAREFN", "焚风矿石精炼厂" },
            { "FAWEAP", "焚风战争工厂" },
            { "FAYARD", "焚风海军船坞" },
            { "FACYBR", "网络核心" },
            { "FACYBR_B", "网络核心扩展模组" },
            { "FACLDP", "穿云尖塔" },
            { "FACLDP_B", "穿云尖塔扩展模组" },
            { "FANANO", "纳米纤维织机" },
            { "FANANO_B", "纳米纤维织机扩展模组" },
            { "FAREPR", "资源再生室" },
            { "FABLST", "爆裂熔炉" },
            { "FAELEV", "暴风起源" },
            { "FAWALL", "防御壁垒" },
            { "FASONI", "声波发射器" },
            { "FAGUAR", "伯劳防空鸟巢" },
            { "FAFILDS", "静滞网格" },
            { "FACONFS", "迷幻网格" },
            { "FARAIL", "轨道炮塔" },
            { "FACOAT", "纳米护甲生成器" },
            { "FACOMP", "离子切割机" },
            { "FAINHI", "信号抑制器" },
            { "FAAVAL", "离子要塞炮" },
            { "FABTRC", "爆裂战壕" },
            { "FAHARB", "先锋导航塔" },
            { "FABARR_B", "轨道舱协议" },
            { "ReconSortieSpecial", "侦查扫描" },
            { "SpinbladeSpecial", "加速旋塔" },
            { "NanofiberSyncSpecial", "纳米同步" },
            { "BlackoutMissileSpecial", "断电导弹" },
            { "MegaarenaSpecial", "巨齿鲨竞技场" },
            { "SignalJammerSpecial", "信号干扰" },
            { "KnightfallSpecial", "天降神兵" },
            { "HarbingerSpecial", "先锋炮艇机" },
            { "NanochargeSpecial", "纳米回复场" },
            { "SweeperDropSpecial", "空降扫荡者" },
            { "BoidBlitzSpecial", "造物惩戒" },
            { "MADMineSpecial", "MAD地雷" },
            { "ChaosTouchSpecial", "迷幻之触" },
            { "DevourerSpecial", "吞并" },
            { "DecoyTeamSpecial", "诱饵小队" },
            { "DecoySquadronSpecial", "诱饵中队" },
            { "GoldenWindSpecial", "黄金之风" },
            { "BlasticadeSpecial", "爆裂屏障" },
            { "GreatTempestSpecial", "风暴起源" },
            { "DEVIL", "红魔鬼" },
            { "JACKALA", "豺狼载具(原型)" },
            { "PROME2", "乳齿象坦克(原型)" },
            { "ATARCHIA", "多智龙火炮(原型)" },
            { "ROACH2", "蛮牛(原型)" },
            { "GHTNK2", "巨鳄载具(原型 )" },
            { "GHTNK2W", "巨鳄载具(原型、水上)" },
            { "ITNK", "传播坦克" },
            { "ADIVER", "爆裂蜂(原型)" },
            { "AHARB", "先锋炮艇机(原型)" },
            { "NANANA", "纳米离心机" },
            { "CACORE", "中枢计算机" },
            { "FAPOST", "发电风带" },
            { "CAGLOB", "全息地球仪" },
            { "CAHOLO01", "焚风投影版A" },
            { "CAHOLO02", "焚风投影版B" },
            { "CAFSUP01", "焚风补给A" },
            { "CAFSUP02", "焚风补给B" },
            { "CAFSUP03", "焚风补给C" },
            { "CAFSUP04", "焚风补给D" },
            { "CAFBRL01", "焚风滚筒A" },
            { "CAFBRL02", "焚风滚筒B" },
            { "CAFBRL03", "焚风滚筒C" },
            { "CANCON01", "纳米纤维贮藏罐A" },
            { "CANCON02", "纳米纤维贮藏罐B" },
            { "CANCON03", "纳米纤维贮藏罐C" },
            { "KNIGHT", "铁骑兵" },
            { "KINGS", "皇家骑兵" },
            { "COVE", "长枪战士" },
            { "RAIL", "轨道炮战士" },
            { "FENGINEER", "焚风工程师" },
            { "HUNTR", "女猎手" },
            { "DEVI", "迷幻猎手" },
            { "CLAIR", "千里眼机器人" },
            { "DUPL", "增殖机器人" },
            { "SYNC", "同步浪人" },
            { "SYNC_N", "同步忍者" },
            { "WASP", "刺蜂行者" },
            { "ZORB", "气压球使徒" },
            { "ZORB_N", "气压球晋升者" },
            { "BANE", "巨人克星" },
            { "BANE_N", "天神克星" },
            { "SICALI", "阿莉兹" },
            { "SIBFIN", "菲因" },
            { "EUREKA", "优莱卡" },
            { "URAGAN", "乌拉甘" },
            { "CAOILD", "科技钻油井" },
            { "CABANK", "科技银行" },
            { "CAEXPS", "科技扩张前哨站" },
            { "CAREFN", "科技矿石精炼厂" },
            { "CAPOWR", "科技电厂" },
            { "CANRCT", "科技核电站" },
            { "CAFDZ", "科技风力发电厂" },
            { "CACOMN", "科技卫星侵入中心" },
            { "CASLAB", "秘密科技实验室" },
            { "CAACAD", "科技步兵学院" },
            { "CAHMCH", "科技重机械厂" },
            { "CAAERO", "科技航空学院" },
            { "CADEFB", "科技防卫局" },
            { "CADOCK", "科技军事码头" },
            { "CAAIRP", "科技机场" },
            { "CAMISL", "科技导弹发射井" },
            { "CASTRF", "科技建筑维护中心" },
            { "CAMACH", "科技机械维修车间" },
            { "CATHOSP", "科技医院" },
            { "CAFHOSP", "科技战地医疗站" },
            { "CATPAD", "科技增援平台" },
            { "CAHMG", "科技重机枪塔" },
            { "CACANN", "科技炮台" },
            { "CASSAM", "科技防空导弹" },
            { "CATUR", "科技加农炮阵地" },
            { "CAART", "科技重型火炮" },
            { "CASAM", "科技防空阵地" },
            { "CASHLD", "科技保护伞" },
            { "CARAD", "科技辐射塔" },
            { "CAWALL", "科技混凝土墙" },
            { "CABUNK01", "科技混凝土碉堡(左下)" },
            { "CABUNK02", "科技混凝土碉堡(右下)" },
            { "CABUNK03", "科技混凝土碉堡(左上)" },
            { "CABUNK04", "科技混凝土碉堡(右上)" },
            { "CAFORT", "科技混凝土要塞" },
            { "NATUNE", "隧道入口" },
            { "NATUNH", "隧道枢纽" },
            { "MaintenanceSpecial", "建筑维护" },
            { "TechMissileSpecial", "导弹攻击" },
            { "CraneSpecial", "维修起重机效果" },
            { "ParaDropSpecial", "伞兵" },
            { "ReinfoPadSpecial", "增援空降" },
            { "CARRELS", "油桶架" },
            { "CAMISC01", "橙色油桶组" },
            { "CAMISC02", "橙色油桶" },
            { "CAMISC01B", "红色油桶组" },
            { "CAMISC02B", "红色油桶" },
            { "CAMISC0X", "凝固汽油桶" },
            { "AMMOCRAT", "弹药箱A" },
            { "CAFTNK01", "燃料罐A" },
            { "CAFTNK02", "燃料罐B" },
            { "CAFTNK03", "燃料罐C" },
            { "CAFTNK04", "燃料罐D" },
            { "CAFTNK05", "燃料罐E" },
            { "CAFTNK06", "燃料罐F" },
            { "CAMISC06", "飞毛腿导弹架" },
            { "PENTGENX", "盟军将军" },
            { "VLADIMIRX", "苏维埃将军" },
            { "BLACKGEN", "厄普西隆将军" },
            { "CARV", "卡维利将军" },
            { "EINS", "阿尔伯特-爱因斯坦" },
            { "PRES", "迈克尔-杜根" },
            { "SSRV", "秘密保镖(金川工业科学家)" },
            { "RMNV", "亚历山大-罗曼诺夫" },
            { "CTECH", "技师" },
            { "CTECHM", "M.A.D.M.A.N.技师" },
            { "CLNT", "Westwood之星" },
            { "ARND", "终结者" },
            { "STLN", "兰博" },
            { "CYCOM", " 机械特种兵" },
            { "MUMY", "木乃伊(被感染的人类)" },
            { "ICE1", "南极浮冰" },
            { "CDUMMY", "工具箱投放" },
            { "BOIDF", "任务标记生成器" },
            { "DMCV", "虚拟基动建设车" },
            { "MDUMMY1", "地图虚拟单位1" },
            { "MDUMMY2", "地图虚拟单位2" },
            { "MDUMMY3", "地图虚拟单位3" },
            { "PDPLANE", "盟军伞兵运输机" },
            { "PDPLANE2", "盟军伞兵运输机" },
            { "PDPLANE3", "苏联伞兵运输机" },
            { "PDPLANE4", "厄普西隆伞兵运输机" },
            { "PDPLANE5", "焚风伞兵运输机" },
            { "PDPLANEUS", "美国运输机" },
            { "CARGOPLANE", "盟军运输机" },
            { "REJU", "空中维修机" },
         

            { "JOSH", "猴子" },
            { "CAML", "骆驼" },
            { "COW", "奶牛" },
            { "BISON", "野牛" },
            { "DEER", "麋鹿" },
            { "TAPIR", "貘" },
            { "HYENA", "鬣狗" },
            { "CDOG", "宠物狗" },
            { "PIG", "猪" },
            { "TIGER", "老虎" },
            { "WTIGR", "白老虎" },
            { "SHEEP", "绵羊" },
            { "ALL", "鳄鱼" },
            { "POLARB", "北极熊" },
            { "GBEAR", "灰熊" },
            { "KANGAROO", "长颈鹿" },
            { "LION", "狮子" },
            { "ELEPHANT", "大象" },
            { "SEL", "海豹" },
            { "DNOA", "沧龙" },
            { "DNOB", "暴龙" },
            { "BEETLE", "巨形甲虫" },
            { "CIV1", "黄衣女性平民" },
            { "CIVX1", "白衣女性平民" },
            { "CIV2", "白衣男性平民A" },
            { "CIVX2", "白衣男性平民B" },
            { "CIV3", "棕衣男性平民" },
            { "CIVX3", "绿衣男性平民" },
            { "CIV4", "德州男性平民A" },
            { "CIVA", "德州男性平民B" },
            { "CIVB", "德州男性平民C" },
            { "CIVC", "德州男性平民D" },
            { "CIVBBR", "棒球运动员平民A" },
            { "CIVBBP", "棒球运动员平民B" },
            { "CIVBFM", "胖沙滩男性平民" },
            { "CIVBTM", "瘦沙滩男性平民" },
            { "CIVBF", "沙滩女性平民A" },
            { "CIVX4", "沙滩女性平民B" },
            { "CIVSFM", "胖雪地男性平民A" },
            { "CIVX7", "胖雪地男性平民B" },
            { "CIVSTM", "瘦雪地男性平民" },
            { "CIVSF", "雪地女性平民A" },
            { "CIVX5", "雪地女性平民B" },
            { "CIVX6", "雪地女性平民C" },
            { "CAR", "汽车A" },
            { "SPGRE", "汽车B" },
            { "REDR12", "汽车C" },
            { "SUV", "汽车D" },
            { "BEETL", "汽车E" },
            { "PRS", "汽车F" },
            { "PRSA", "汽车G" },
            { "YCAB", "汽车H" },
            { "EUROC", "汽车I" },
            { "SUVW", "汽车J" },
            { "BCAB", "汽车K" },
            { "SUVB", "汽车L" },
            { "STANG", "汽车M" },
            { "BUS", "校车" },
            { "WINI", "冷藏车" },
            { "PICK", "皮卡车" },
            { "PTRUCK", "无货物皮卡车" },
            { "TRUCKA", "卡车(空)" },
            { "TRUCKB", "卡车(载货)" },
            { "PROPA", "宣传车" },
            { "CONA", "挖掘机" },
            { "DIGG", "迷你挖掘机" },
            { "COP", "警车" },
            { "COPCAR", "警用货车" },
            { "LIMO", "豪华轿车" },
            { "TAXI", "美国出租车" },
            { "TAXI2", "欧洲出租车" },
            { "ARGT", "阿根廷出租车" },
            { "DDBX", "双层巴士" },
            { "JEEP", "吉普车" },
            { "DOLY", "好莱坞摄影车" },
            { "FTRK", "消防车A" },
            { "FTRUCK", "消防车B" },
            { "AMBL", "救护车A" },
            { "AMBU", "救护车B" },
            { "WZHDL", "橙色卡车" },
            { "TRASH", "垃圾车" },
            { "CONVEH", "建设单位" },
            { "CBLC", "有轨电车" },
            { "SFGP50L", "圣菲达GP50" },
            { "SFCF7R", "圣菲达CF7" },
            { "SFGP60L", "圣菲达GP60M" },
            { "SFB237I", "圣菲达B23-7" },
            { "SFC408WL", "圣菲达C40-8W" },
            { "SFCAB", "圣菲达车尾" },
            { "STEAM", "火车" },
            { "SSUBCAR", "火车车厢" },
            { "MIXER", "混凝土搅拌车" },
            { "PLOW", "扫雪车" },
            { "BLDZ", "推土机" },
            { "SDOZ", "小型推土机" },
            { "DOZER", "大型推土机" },
            { "DRLLTRCK", "钻车" },
            { "BOBC", "山猫" },
            { "FLOAD", "前端装载机A" },
            { "FLOADB", "前端装载机B" },
            { "RDROLLER", "压路机" },
            { "ROLLER", "蒸汽压路机" },
            { "SMOB", "雪地机动车" },
            { "GROOMER", "压雪机" },
            { "FARM", "拖拉机" },
            { "MACK", "马克卡车" },
            { "TOW", "拖车" },
            { "CIVP", "客机" },
            { "PROPPL", "螺旋桨飞机A" },
            { "PROPPLB", "螺旋桨飞机B" },
            { "SPORT", "跑车A" },
            { "SPORTB", "跑车B" },
            { "SPORTC", "跑车C" },
            { "SPORTD", "跑车D" },
            { "SPORTE", "跑车E" },
            { "SPORTF", "跑车F" },
            { "SPORTG", "跑车G" },
            { "GIGA", "巨型怪兽车" },
            { "MTRUCK", "怪物卡车A" },
            { "MTRUCKB", "怪物卡车B" },
            { "MTRUCKC", "怪物卡车C" },
            { "CREAM", "冰淇凌车" },
            { "OILTRUCK", "油罐车" },
            { "FAFILD", "静滞网格(衍生)" },
            { "KnightfallBeacon", "天降神兵信标" },
            { "FACOMPAI", "离子切割机(AI)" },
            { "FACONF", "迷幻网格(衍生)" },
            { "FABARRB", "焚风兵营(AI)" },
            { "CANMIN", "水雷" },
            { "FAINHIB", "信号抑制器(AI)" },
            { "FAJAMM", "信号干扰器(一次性)" },
            { "FASPIN", "加速旋塔" },
            { "FASPINAI", "加速旋塔(AI)" },
            { "FAWEAPB", "焚风战车工厂(AI)" },
            { "FAYARDAI", "焚风船坞(AI)" },
            { "GAPILEB", "盟军兵营(AI)" },
            { "GAWEAPB", "盟军战车工厂(AI)" },
            { "GAYARDAI", "盟军船坞(AI)" },
            { "GAROD", "聚能针" },
            { "NAHANDB", "苏联兵营(AI)" },
            { "NAWEAPB", "苏联战车工厂(AI)" },
            { "NAYARDAI", "苏联船坞(AI)" },
            { "NAAIR", "苏联空军基地" },
            { "NADRON", "维修起重机"},
            { "NAFIST", "斯大林之拳(部署)"},
            { "NAFTUR", "烈焰炮塔" },
            { "NAGATE_A", "苏联闸门(南北)" },
            { "NAGATE_B", "苏联闸门(东西)" },
            { "NAHAMM", "地锤防御装置" },
            { "NADIST", "裂解防空塔" },
            { "NASCOM", "侦测塔" },
            { "NAIRDM", "钢铁守卫" },
            { "NANRCTUP", "核能转换器" },
            { "NAPRIS", "战地情报局" },
            { "YAPOWRB", "生化反应室(AI)" },
            { "NACNST", "苏联建造场" },
            { "NAGRUM", "轰鸣防空导弹(部署)" },
            { "NATECHR", "宫殿" }, 
            { "NAFURY", "怒焰地雷无人机(部署)" },
            { "NAMORT", "烟雾炮台" },
            { "NATECHC", "作战实验室" },
            { "NAEMPS", "EMP控制站" },
            { "NATEK", "原子核心" },
            { "NATRAPS", "EMP地雷" },
            { "NATRAP", "EMP地雷(衍生)" },
            { "-EMPMineSpawn", "EMP地雷投放" },

           
            { "FAAREN", "巨齿鲨强化塔" },
            { "FAMSA", "机动声波侦测仪(部署)" },
            { "FAMSAW", "机动声波侦测仪(部署、水上)" },
            { "FADROP", "轨道舱信标" }, 
            { "FABOID", "机械造物(部署)" },
            { "FAORCI", "虎鲸机动声雷达阵列(部署)" },
            { "FAMMIN", "M.A.D地雷" },
            { "FASWPR", "扫荡者无人机(部署)" },
            { "NABNKR", "战斗碉堡" },
            { "NAFLAK", "高射炮" },
            { "NAHAND", "苏联兵营" },
            { "NAINDP", "工业工厂" },
            { "NAIRON", "铁幕装置" },
            { "NALASR", "哨戒机炮" },
            { "NAMISL", "核弹发射井" },
    
            { "NARADR", "雷达基站" },
            { "NAREFN", "苏联矿石精炼厂" },
            { "NAWALL", "苏联要塞墙" },
            { "NAWEAP", "苏联战争工厂" },
            { "NAYARD", "苏联海军船坞" },
            { "NAPOWR", "磁能反应炉" },
            { "NANRCT", "核子反应炉" }, 
            { "NAPSYB", "厄普西隆心灵信标" },
            { "NAPSYB2", "厄普西隆心灵信标" },

            // 易燃易爆
            

// 战役特有建筑
            { "CAPP", "煤炭发电厂" },
            { "CADOME", "雷达球" },
            { "GAAIRB", "旧雷达球A" },
            { "GAAIRB2", "旧雷达球B" },
            { "GAAIRB3", "旧雷达球C" },
            { "GAAIRB4", "旧雷达球D" },
            { "GAAIRB5", "旧雷达球E" },
            { "CAMINE", "矿井" },
            { "CAOILR", "石油精炼厂" },

// 战役换皮建筑
            { "CASBUN", "科技支援碉堡" },
            { "CAMISC01T", "绿色油桶组" },
            { "CAMISC02T", "绿色油桶" },

// 其他换皮建筑
            { "ZTARGETZ", "坐标" },
            { "NARECL", "维修设施" },
            { "KEYBLD", "末日装置" },

// 其他建筑
            { "CAMSC13", "废弃猛犸坦克" },
            { "RADFLD1", "大型辐射区" },
            { "RADFLD2", "小型辐射区" },
            { "AILOCK", "AI锁科技建筑" },
            { "CACEAS", "停火发生器" },
            { "CACRSS1", "自定义道具A" },
            { "CACRSS2", "自定义道具B" },
            { "CACRSS3", "自定义道具C" },
            { "CACRSS4", "自定义道具D" },
            { "CACRSSX", "自定义道具X" },
            { "CASHTAKE", "虚拟夺钱建筑" },
            { "CASHGIVE", "虚拟给钱建筑" },
            { "NOTHIN", "无关虚拟建筑" },
            { "DUMMYDUMMY", "普通虚拟建筑" },
            { "CASHROUD", "地图遮盖发射器" },
            { "CABHUT", "桥梁维修小屋" },
            { "ORCINX", "虎鲸波动干扰仪(无敌)" },

// 其他支援技能
            { "PsychicBeaconSpecial", "心灵信标启动" },
            { "SuperFlashSpawn", "全局闪光超武" },
            { "NukeCloneSpecial", "对地战术导弹攻击" },
            { "TimeFreezeSpecial", "时间静止" },
            { "CeasefireSpawn", "停火放置" },
            { "CeasefireSpecial", "停火" },
            { "JudgementSpecial", "审判" },
            { "CrateDropSpecial", "工具箱投放效果" },
            { "Deploy1Special", "自定义猎杀机器人1" },
            { "Deploy2Special", "自定义猎杀机器人2" },
            { "Deploy3Special", "自定义猎杀机器人3" },
            { "Deploy4Special", "自定义猎杀机器人4" },
            { "Team1Special", "自定义超时空传送1" },
            { "Team2Special", "自定义超时空传送2" },
            { "Team3Special", "自定义超时空传送3" },
            { "Team4Special", "自定义超时空传送4" },
            { "DropPodSpawn1", "自定义空降仓1" },
            { "DropPodSpawn2", "自定义空降仓2" },
            { "DropPodSpawn3", "自定义空降仓3" },
            { "DropPodSpawn4", "自定义空降仓4" },
            { "WarheadSpecial1", "自定义弹头1" },
            { "WarheadSpecial2", "自定义弹头2" },
            { "WarheadSpecial3", "自定义弹头3" },
            { "WarheadSpecial4", "自定义弹头4" },
            { "IonStormSpecial", "终极武器" },

// 平民飞行器
            { "PASPLN1", "客机A" },
            { "PASPLN2", "客机B" },

            // 平民海军
            { "BOAT", "船A" },
            { "BOATB", "船B" },
            { "BOATC", "船C" },
            { "FBOAT", "渔船" },
            { "OILTANKER", "油轮" },
            { "CRUISE", "游轮" },
            { "TUG", "拖船" },

// 旗帜、光柱与广告牌（仅举例部分，全部可继续补充）
            { "CAUSFGL", "美国国旗" },
            { "CAEAFLG", "欧盟旗帜" },
            { "CAPFFLG", "线旗帜" },
            { "CARUFGL", "苏联国旗" },
            { "CALCFGL", "拉丁同盟旗帜" },
            { "CACHIN", "中国国旗" },
            { "CAPBAN", "心灵军团旗帜" },
            { "CASBAN", "天蝎组织旗帜" },
            { "CAHBAN", "总部守卫旗帜" },
            { "CAHHFLG", "狂鲨先锋旗帜" },
            { "CAWCFLG", "科洛尼亚侧翼旗帜" },
            { "CALBFLG", "最后的堡垒旗帜" },
            { "CAFRFGL", "法国国旗" },
            { "CAUKFGL", "英国国旗" },
            { "CAGEFGL", "德国国旗" },
            { "CASKFGL", "韩国国旗" },
            { "CAJPFLG", "日本国旗" },
            { "CAPOFGL", "波兰国旗" },
            { "CAIFLG", "意大利国旗" },
            { "CAARFGL", "阿根廷国旗" },
            { "CACUFGL", "古巴国旗" },
            { "CALBFGL", "利比亚国旗" },
            { "CAIRFGL", "伊拉克国旗" },
            { "CASPAFGL", "西班牙国旗" },
            { "CAGREFGL", "希腊国旗" },
            { "CANRKR", "朝鲜国旗" },
            { "CAAFLG", "盟军军旗" },
            { "REDLAMP", "红色光柱" },
            { "GRENLAMP", "绿色光柱" },
            { "BLUELAMP", "蓝色光柱" },
            { "YELWLAMP", "黄色光柱" },
            { "PURPLAMP", "紫色光柱" },
            { "INORANLAMP", "隐形橙色光柱" },
            { "INGRNLMP", "隐形绿色光柱" },
            { "INREDLMP", "隐形红色光柱" },
            { "INBLULMP", "隐形蓝色光柱" },
            { "INGALITE", "隐形白色光柱" },
            { "INYELWLAMP", "隐形黄色光柱" },
            { "INPURPLAMP", "隐形紫色光柱" },
            { "INTEALLAMP", "隐形青色光柱" },
            { "INMAGNLAMP", "隐形品红色光柱" },
            { "INBLCKLAMP", "隐形黑色光柱" },
            { "INAQUALAMP", "隐形浅蓝色光柱" },
            { "INPINKLAMP", "隐形粉色光柱" },
            { "NEGBLUE", "淡黄色光柱" },
            { "NEGRED", "淡青色光柱" },
            { "NEGGREEN", "淡紫色光柱" },
            { "REDTLAMP", "淡红色光柱" },
            { "GREENTLAMP", "淡绿色光柱" },
            { "TEMMORLAMP", "温和晨光光柱" },
            { "TEMDAYLAMP", "温和日光光柱" },
            { "TEMDUSLAMP", "温和黄昏光柱" },
            { "TEMNITLAMP", "温和夜间光柱" },
            { "SNOMORLAMP", "雪地早晨光柱" },
            { "SNODAYLAMP", "雪地白昼光柱" },
            { "SNODUSLAMP", "雪地黄昏光柱" },
            { "SNONITLAMP", "雪地夜间光柱" },
            { "NEGLAMP", "负白色光柱" },
            { "NEGBLACK", "深黑色光柱" },
            { "TSTLAMP", "α光圆形A" },
            { "TSTLAMPB", "α光圆形B" },
            { "TSTLAMPC", "α光方形A" },
            { "TSTLAMPD", "α光方形A" },
            { "XLAMP", "负α光圆形" },
            { "XLAMPB", "负α光方形" },
            { "CAMOV01", "香蕉显示屏" },
            { "CAMOV03", "蚂蚁显示屏" },
            { "CAMOV02", "电影荧屏" },
            { "CAYUNO", "CnCNet广告牌A" },
            { "CAYUNO2", "CnCNet广告牌B" },
            { "CAASUK", "ARES广告牌A" },
            { "CAASUK2", "ARES广告牌B" },
            { "CAMDDB", "ModDB广告牌" },
            { "CAMDDB2", "ModDB广告牌" },
            { "CALA10", "Westwood广告牌" },
            { "CALB10", "Westwood广告牌" },
            { "CAMIKU", "WorldBeyond广告牌A" },
            { "CAMIKU2", "WorldBeyond广告牌B" },
            { "CADTBG", "扩展包广告牌A" },
            { "CADTBG2", "扩展包广告牌B" },
            { "CAKIND", "金川工业广告牌A" },
            { "CAKIND2", "金川工业广告牌B" },
            { "CAKNIN", "金川国际部广告牌A" },
            { "CAKNIN2", "金川国际部广告牌B" },
            { "CASTCH", "命运科技广告牌A" },
            { "CASTCH2", "命运科技广告牌B" },
            { "CABILL01A", "服从尤里广告牌" },
            { "CABILL01B", "服从尤里广告牌" },
            { "CABILL02A", "铁锤镰刀广告牌" },
            { "CABILL02B", "铁锤镰刀广告牌" },
            { "CABILL03A", "苏维埃世界广告牌" },
            { "CABILL03B", "苏维埃世界广告牌" },
            { "CABILL04A", "加入尤里广告牌" },
            { "CABILL04B", "加入尤里广告牌" },
            { "CABILL05A", "苏军广告牌" },
            { "CABILL05B", "苏军广告牌" },
            { "CABILL06A", "使命召唤广告牌" },
            { "CABILL06B", "使命召唤广告牌" },
            { "CABILL07A", "罗曼诺夫广告牌" },
            { "CABILL07B", "罗曼诺夫广告牌" },
            { "CABILL08A", "尤里和罗曼诺夫广告牌" },
            { "CABILL08B", "尤里和罗曼诺夫广告牌" },
            { "CABILL09A", "苏联宇航员广告牌" },
            { "CABILL09B", "苏联宇航员广告牌" },
            { "CABILL10A", "苏联太空广告牌" },
            { "CABILL10B", "苏联太空广告牌" },
            { "CABILL11A", "列宁广告牌" },
            { "CABILL11B", "列宁广告牌" },
            { "DBMOV", "黑之契约者广告牌" },
            { "CAASHN", "ashens.com广告牌" },
            { "CASTEI", "命运石之门广告牌" },
            { "CASTEI2", "命运石之门广告牌" },
            { "CAWOAH01", "哇，这是一个广告牌" },
            { "CAWOAH02", "哇，这是一个广告牌" },
            { "GASAND", "沙袋" },
            { "CABARR01", "拒马" },
            { "CABARR02", "拒马" },
            { "CARDBK01", "路障" },
            { "CARDBK02", "路障" },
            { "CARDBK03", "路障" },
            { "CARDBK04", "路障" },
            { "CABARB", "铁丝网" },
            { "CAFNCP", "监狱铁栅栏" },
            { "CAFNCB", "黑色栅栏" },
            { "CAFNCW", "白色栅栏" },
            { "CAWASH02", "华盛顿建筑" },
            { "CAWASH03", "华盛顿建筑" },
            { "CAWASH04", "华盛顿建筑" },
            { "CAWASH05", "华盛顿建筑" },
            { "CAWASH06", "华盛顿建筑" },
            { "CAWASH07", "华盛顿建筑" },
            { "CAWASH08", "华盛顿建筑" },
            { "CAWASH09", "华盛顿建筑" },
            { "CAWASH10", "华盛顿建筑" },
            { "CAWASH11", "华盛顿建筑" },
            { "CAWSH12", "华盛顿纪念碑" },
            { "CAWASH13", "华盛顿建筑" },
            { "CAWASH14", "杰弗逊纪念馆" },
            { "CAWASH15", "林肯纪念馆" },
            { "CAWASH16", "史密森尼古堡" },
            { "CAWASH17", "史密森尼自然历史博物馆" },
            { "CAWASH18", "白宫喷泉池" },
            { "CACPTL", "美国国会" },
            { "CANEWY01", "纽约建筑物" },
            { "CANWY05", "世贸中心" },
            { "CANEWY06", "华尔街办公楼" },
            { "CANEWY07", "华尔街办公楼" },
            { "CANEWY08", "华尔街办公楼" },
            { "CANWY09", "纽约建筑" },
            { "CANEWY10", "纽约建筑" },
            { "CANEWY11", "纽约建筑" },
            { "CANEWY12", "纽约建筑" },
            { "CANEWY13", "纽约建筑" },
            { "CANEWY14", "纽约建筑" },
            { "CANEWY15", "纽约建筑" },
            { "CANEWY16", "纽约建筑" },
            { "CANEWY17", "纽约建筑" },
            { "CANEWY18", "纽约建筑" },
            { "CANEWY20", "仓库" },
            { "CANEWY21", "仓库" },
            { "CANWY22", "纽约建筑" },
            { "CANWY23", "纽约建筑" },
            { "CANWY24", "纽约建筑" },
            { "CANWY25", "纽约建筑" },
            { "CANWY26", "纽约建筑" },
            { "CACHIG01", "芝加哥砖房" },
            { "CACHIG02", "芝加哥砖房" },
            { "CACHIG03", "芝加哥办公楼" },
            { "CACHIG04", "芝加哥交流中心" },
            { "CACHIG05", "希尔斯大厦" },
            { "CACHIG06", "芝加哥水塔" },
            { "CATEXS01", "得克萨斯建筑" },
            { "CATEXS02", "阿拉莫要塞" },
            { "CATEXS03", "圣安东尼奥办公楼" },
            { "CATEXS04", "圣安东尼奥办公楼" },
            { "CATEXS05", "圣安东尼奥办公楼" },
            { "CATEXS06", "得克萨斯办公楼" },
            { "CATEXS07", "得克萨斯办公楼" },
            { "CATEXS08", "得克萨斯办公楼" },
            { "CATEXS08B", "得克萨斯办公楼" },
            { "CAMIAM01", "迈阿密旅馆" },
            { "CAMIAM02", "迈阿密旅馆" },
            { "CAMIAM03", "迈阿密旅馆" },
            { "CAMIAM04", "救生员亭" },
            { "CAMIAM05", "迈阿密旅馆" },
            { "CAMIAM06", "迈阿密旅馆" },
            { "CAMIAM07", "迈阿密旅馆" },
            { "CAMIAM08", "亚利桑那纪念馆" },
            { "CALA01", "洛杉矶建筑" },
            { "CALA03", "好莱坞标志" },
            { "CALA04", "好莱坞碗形剧场" },
            { "CALA06", "洛杉矶塔台" },
            { "CALA07", "电影院" },
            { "CALA08", "汽车代理商" },
            { "CALA09", "便利商店" },
            { "CALA11", "好莱坞剧场观众席" },
            { "CALA12", "好莱坞剧场观众席" },
            { "CALA13", "好莱坞标志" },
            { "CALA15", "小型购物中心" },
            { "CASANF01", "旧金山建筑" },
            { "CASANF02", "旧金山建筑" },
            { "CASANF03", "旧金山建筑" },
            { "CASANF04", "金门大桥A" },
            { "CASANF05", "恶魔岛建筑" },
            { "CASANF06", "旧金山建筑" },
            { "CASANF07", "旧金山建筑" },
            { "CASANF08", "旧金山建筑" },
            { "CASANF09", "金门大桥B" },
            { "CASANF10", "金门大桥C" },
            { "CASANF11", "金门大桥D" },
            { "CASANF12", "金门大桥E" },
            { "CASANF13", "金门大桥F" },
            { "CASANF14", "金门大桥G" },
            { "CASANF15", "旧金山恶魔岛水塔" },
            { "CASANF16", "旧金山恶魔岛灯塔" },
            { "CASANF17", "旧金山建筑" },
            { "CASANF18", "旧金山建筑" },
            { "CASEAT01", "西雅图太空针塔" },
            { "CASEAT02", "巨软园区" },
            { "CASTL01", "圣路易斯建筑" },
            { "CASTL02", "圣路易斯建筑" },
            { "CASTL03", "圣路易斯建筑" },
            { "CASTL04", "圣路易斯拱门" },
            { "CADSTA", "圆顶建筑" },
            { "CAONIO", "圆顶建筑" },
            { "CAORAN", "圆顶建筑" },
            { "CAWSTA", "铂金体育馆" },
            { "CABSTA", "青铜体育馆" },
            { "CAOSTA", "蓝宝石体育馆" },
            { "CASTL05A", "布施体育馆A" },
            { "CASTL05B", "布施体育馆B" },
            { "CASTL05C", "布施体育馆C" },
            { "CASTL05D", "布施体育馆D" },
            { "CASTL05E", "布施体育馆E" },
            { "CASTL05F", "布施体育馆F" },
            { "CASTL05G", "布施体育馆G" },
            { "CASTL05H", "布施体育馆H" },
            { "CAHSE01", "美国房屋" },
            { "CAHSE02", "美国房屋" },
            { "CAHSE03", "美国房屋" },
            { "CAHSE04", "美国房屋" },
            { "CAHSE05", "美国货柜屋" },
            { "CAHSE06", "美国货柜屋" },
            { "CAHSE07", "美国房屋" },
            { "CASTAT01", "雕像" },
            { "CASTAT02", "雕像" },
            { "CASTAT03", "雕像" },
            { "CASTAT04", "雕像" },
            { "CASTAT05", "雕像" },
            { "CASTAT06", "雕像" },
            { "CASTAT07", "雕像" },
            { "CAEUR1", "欧洲小屋" },
            { "CAEUR2", "欧洲小屋" },
            { "CAEUR04", "欧洲建筑" },
            { "CAEURO05", "雕像" },
            { "CAPARS02", "巴黎大型建筑" },
            { "CAPRS03", "卢浮宫" },
            { "CAPARS04", "巴黎建筑" },
            { "CAPARS05", "巴黎建筑" },
            { "CAPARS06", "巴黎建筑" },
            { "CAPARS07", "欧洲电话亭" },
            { "CAPARS08", "巴黎大型建筑" },
            { "CAPARS09", "巴黎大型建筑" },
            { "CAPARS10", "巴黎餐馆" },
            { "CAPARS11", "巴黎凯旋门" },
            { "CAPARS12", "巴黎圣母院" },
            { "CAPARS13", "巴黎餐馆" },
            { "CAPARS14", "巴黎餐馆" },
            { "CALOND01", "伦敦建筑" },
            { "CALOND03", "伦敦酒吧" },
            { "CALOND04", "英国国会" },
            { "CALOND05", "大本钟" },
            { "CALOND06", "伦敦塔" },
            { "CACITY05", "巴特西发电站" },
            { "CACOLO", "罗马斗兽场" },
            { "CAKRMW", "克里姆林宫砖墙" },
            { "CARUS01", "圣巴索大教堂" },
            { "CARUS02A", "克里姆林宫塔" },
            { "CARUS02B", "克里姆林宫塔" },
            { "CARUS02C", "克里姆林宫北城墙" },
            { "CARUS02D", "克里姆林宫东城墙" },
            { "CARUS02E", "克里姆林宫南城墙" },
            { "CARUS02F", "克里姆林宫西城墙" },
            { "CARUS02G", "克里姆林宫钟楼" },
            { "CARUS03", "克里姆林宫" },
            { "CARUS04", "莫斯科建筑" },
            { "CARUS05", "莫斯科建筑" },
            { "CARUS06", "莫斯科建筑" },
            { "CARUS07", "红场圆台" },
            { "CARUS08", "苏联墙" },
            { "CARUS09", "苏联墙" },
            { "CARUS10", "苏联墙" },
            { "CARUS11", "苏联墙" },
            { "CAMAUS", "列宁墓" },
            { "CAHERT", "苏维埃宫" },
            { "CATSAR", "沙皇钟" },
            { "CAPALACE01", "凯瑟琳宫" },
            { "CAPALACE02", "凯瑟琳宫" },
            { "CAPALACE03", "凯瑟琳宫" },
            { "CAPALACE04", "凯瑟琳宫" },
            { "CARUSHT", "莫斯科历史博物馆" },
            { "CABORS", "鲍里斯雕像" },
            { "CAPOZN", "波兹南竞技场" },
            { "CAWSIR", "华沙美人鱼" },
            { "CASEIM01", "波兰国会" },
            { "CASEIM02", "波兰国会" },
            { "CASEIM03", "波兰国会" },
            { "CASEIM04", "波兰国会" },
            { "CASEIM05", "波兰国会" },
            { "CASEIM06", "波兰国会" },
            { "CATRAN03", "尤里要塞" },
            { "CATRAN01", "地窖" },
            { "CATRAN02", "地窖" },
            { "CABARR", "反抗军避难所" },
            { "CAEGYP01", "埃及大金字塔" },
            { "CAEGYP02", "埃及小金字塔" },
            { "CAEGYP03", "斯芬克斯像" },
            { "CAEGYP04", "埃及大金字塔" },
            { "CAEGYP05", "埃及大金字塔" },
            { "CAEGYP06", "埃及大金字塔" },
            { "CAMORR01", "摩洛哥建筑" },
            { "CAMORR02", "摩洛哥建筑" },
            { "CAMORR03", "摩洛哥建筑" },
            { "CAMORR04", "摩洛哥建筑" },
            { "CAMORR05", "摩洛哥酒吧" },
            { "CAMORR06", "里克酒馆" },
            { "CAMORR07", "摩洛哥建筑" },
            { "CAMORR08", "摩洛哥建筑" },
            { "CAMORR09", "摩洛哥建筑" },
            { "CAMORR10", "摩洛哥建筑" },
            { "CAOPER", "特内里费歌剧院" },
            { "CASYDN02", "袋鼠汉堡店" },
            { "CASYDN03", "悉尼歌剧院" },
            { "CATOKY", "东京塔" },
            { "CAJAPA01", "日本建筑物" },
            { "CAJAPA02", "日本建筑物" },
            { "CAJAPA03", "日本建筑物" },
            { "CAJAPA04", "日本建筑物" },
            { "CAJAPA05", "日本建筑物" },
            { "CAJAPA06", "日本墙" },
            { "CAJAPA07", "日本建筑物" },
            { "CAJAPA08", "日本建筑物" },
            { "CAJAPA09", "日本建筑物" },
            { "CAJAPA10", "日本建筑物" },
            { "CAJAPA11", "日本建筑物" },
            { "CACOLM", "日本圆柱" },
            { "CAJORA", "神龛" },
            { "CCHINA01", "天安门" },
            { "CCHINA02", "故宫" },
            { "CAHEAV", "天坛" },
            { "CACHNA01", "中国建筑物" },
            { "CACHNA02", "中国建筑物" },
            { "CACHNA03", "中国塔" },
            { "CAPRLT", "东方明珠塔" },
            { "CABUDD", "佛像" },
            { "CAFISH", "鱼雕像" },
            { "CADRAG01", "龙尾" },
            { "CADRAG02", "龙门" },
            { "CADRAG03", "龙门" },
            { "CADRAG04", "龙头" },
            { "SLHYT", "龙形雕像" },
            { "MAYAN", "玛雅金字塔" },
            { "CAMEX01", "玛雅金字塔" },
            { "CAMEX02", "玛雅遗迹" },
            { "CAMEX03", "玛雅遗迹" },
            { "CAMEX04", "玛雅遗迹" },
            { "CAMEX05", "玛雅遗迹" },
            { "NAAZT1", "玛雅神庙" },
            { "NAAZT2", "玛雅神庙" },
            { "CAEAST01", "复活节岛石像" },
            { "CATIKI01", "提基神像" },
            { "CATIKI02", "提基神像" },
            { "CATIKI03", "提基雕像" },
            { "CATIKI04", "提基雕像" },
            { "CATIKI05", "提基雕像" },
            { "CATIKI06", "提基雕像" },
            { "CATIKI07", "提基雕像" },
            { "CATIKI08", "提基雕像" },
            { "CATOTE01", "图腾" },
            { "CATOTE02", "图腾" },
            { "CASHSE01", "房屋" },
            { "CASHSE02", "砖厂" },
            { "CASHSE03", "工厂" },
            { "CACHUR01", "教堂" },
            { "CACHUR02", "教堂" },
            { "CACHUR03", "克罗地亚圣詹姆斯教堂" },
            { "CACHUR04", "教堂废墟" },
            { "CACHUR05", "教堂废墟" },
            { "CACHUR06", "教堂" },
            { "CACHUR07", "教堂" },
            { "CACHUR08", "教堂" },
            { "CACHUR09", "避难所" },
            { "CAMUSE01", "公馆" },
            { "CAMUSE02", "公馆" },
            { "CAMUSE03", "公馆" },
            { "CAMUSE04", "公馆" },
            { "CAMONA", "寺院" },
            { "CASANT", "圣天使堡" },
            { "CAPETE", "圣彼得宫" },
            { "CARING", "圣彼得广场" },
            { "CARING2", "圣彼得广场" },
            { "CAPALA", "维恩庄园" },
            { "BANK", "银行" },
            { "CAMALL", "购物中心" },
            { "CASWST01", "西南建筑物" },
            { "CALUNR01", "登月舱" },
            { "CALUNR02", "美国月球旗" },
            { "CAYBAN", "厄普西隆横幅" },
            { "CABANN01", "日本横幅" },
            { "CABANN02", "日本横幅" },
            { "CAHELI", "直升机停机坪" },
            { "MOMOV", "命令与征服横幅" },
            { "CAOWLF", "猫头鹰与朋友横幅" },
            { "CASTN01", "火车站" },
            { "CASTN02", "火车站" },
            { "CAPLF01", "火车站台" },
            { "CAPLF02", "火车站台" },
            { "CAFONT", "喷泉池" },
            { "CACAST", "城堡" },
            { "CAEXCV", "考古地" },
            { "CASTON", "石头雕塑" },
            { "CASTON01", "石头雕塑" },
            { "CASTON02", "石头雕塑" },
            { "CAMONU", "方尖碑" },
            { "CACANN01", "大炮" },
            { "CACANN02", "大炮" },
            { "CACNONA", "古代火炮" },
            { "CACNONB", "古代火炮" },
            { "CABALL", "炮弹" },
            { "CASCIE01", "研究组件" },
            { "CASCIE02", "研究组件" },
            { "CASCIE03", "研究组件" },
            { "CASCIE04", "研究组件" },
            { "CASHIP", "废弃船只" },
            { "CASHIP01", "废弃船只" },
            { "CASHIP02", "废弃船只" },
            { "CASTOR", "仓库" },
            { "CASTOR02", "大仓库" },
            { "CAPLAT01", "被毁平台" },
            { "CAOPLT", "石油钻井平台" },
            { "CAOPLT2", "大型石油钻井平台" },
            { "CAARMY01", "军队营帐" },
            { "CAARMY02", "军队营帐" },
            { "CAARMY03", "军队营帐" },
            { "CAARMY04", "军队营帐" },
            { "CATENT02", "帐篷" },
            { "CATENT03", "帐篷" },
            { "CATENT04", "帐篷" },
            { "CATENT05", "帐篷" },
            { "CATENT06", "帐篷" },
            { "CATENT07", "帐篷" },
            { "CAINDU01", "工厂废墟" },
            { "CAINDU02", "工厂废墟" },
            { "CAINDU03", "工厂废墟" },
            { "CAINDU04", "工厂废墟" },
            { "CAINDU05", "工厂废墟" },
            { "CAINDU06", "工厂废墟" },
            { "CAINDU07", "工厂废墟" },
            { "CAMINE01", "采矿设施" },
            { "CAMINE02", "会所" },
            { "CAMINE03", "会所" },
            { "CAMINE04", "采矿钻" },
            { "CABRID", "桥" },
            { "CADRIL", "旧钻头" },
            { "CACRYO", "圆顶研究室" },
            { "CACRYOB", "小圆顶研究室" },
            { "CATEKN01", "实验室" },
            { "CATEKN02", "旧实验室" },
            { "CAHTCH", "储油库仓门" },
            { "CAFUEL", "储油罐" },
            { "CAOLEO", "输油管道" },
            { "CASOLR", "太阳能电池板" },
            { "CAVENT", "通风设施" },
            { "CACMNT", "无线电塔" },
            { "CAGARD", "守卫塔" },
            { "CAGARD01", "苏联警戒哨" },
            { "CAGARD02", "盟军警戒哨" },
            { "CACHUT", "检查站" },
            { "GAGATE_A", "闸门" },
            { "CACARG1", "盟军集装箱" },
            { "CACARG2", "盟军集装箱" },
            { "CASCRG01", "苏联集装箱" },
            { "CASCRG02", "苏联集装箱" },
            { "CARSIN01", "核辐射警示牌" },
            { "CARSIN02", "核辐射警示牌" },
            { "CASIN01E", "禁行标志" },
            { "CASIN01W", "禁行标志" },
            { "CASIN01S", "禁行标志" },
            { "CASIN01N", "禁行标志" },
            { "CAPOL01E", "电线杆" },
            { "CAPOL01N", "电线杆" },
            { "CAPOL01S", "电线杆" },
            { "CAPOL01W", "电线杆" },
            { "CAWIND", "风车" },
            { "CAWT01", "水塔" },
            { "CAJWAT", "旧水塔" },
            { "CAWATR", "水塔" },
            { "CABUBL", "水井" },
            { "CATS01", "谷仓" },
            { "CABARN02", "谷仓" },
            { "CAFARM01", "农庄" },
            { "CAFARM02", "农场贮槽" },
            { "CAFARM06", "灯塔" },
            { "CAFRMA", "农舍" },
            { "CAFRMB", "移动式厕所" },
            { "CAGAS01", "加油站" },
            { "CAMISC03", "垃圾箱" },
            { "CAMISC04", "邮筒" },
            { "CAMISC05", "水管" },
            { "CAABOX", "箱子" },
            { "CACTNT", "工人帐篷" },
            { "CAPHUT", "海滩房屋" },
            { "CAPHUT2", "海滩小屋" },
            { "CAPHUT3", "海滩小屋" },
            { "CAPHUT4", "海滩小屋" },
            { "CAMSC01", "热狗摊" },
            { "CAMSC02", "海滩遮阳伞" },
            { "CAMSC03", "海滩遮阳伞" },
            { "CAMSC04", "海滩毛巾" },
            { "CAMSC05", "海滩毛巾" },
            { "CAMSC06", "篝火" },
            { "CAMSC07", "茅草屋" },
            { "CAMSC08", "茅草屋" },
            { "CAMSC09", "茅草屋" },
            { "CAMSC10", "汉堡王" },
            { "CAMSC11", "轮胎" },
            { "CAMSC12", "练习靶" },
            { "CAMSC12A", "练习靶" },
            { "CUTARGET", "人形练习靶" },
            { "CAPICN01", "野餐桌" },
            { "CAPICN02", "野餐桌" },
            { "CAPARK01", "公园长椅" },
            { "CAPARK02", "秋千" },
            { "CAPARK03", "公园转盘" },
            { "CAPARK04", "公园长椅" },
            { "CAPARK05", "公园长椅" },
            { "CAPARK06", "公园长椅" },
            { "FEYRIS", "摩天轮" },
            { "CASTRT01", "交通灯" },
            { "CASTRT02", "交通灯" },
            { "CASTRT03", "交通灯" },
            { "CASTRT04", "交通灯" },
            { "CASTRT05", "公交车站" },
            { "CABUS01", "公交车站" },
            { "CABUS02", "公交车站" },
            { "CADFLY", "蜻蜓雕像" },
            { "CAZOO", "动物园" },
            { "CABIOS", "考古学博物馆" },
            { "CAPRSNA", "监狱" },
            { "CAPRSNB", "监狱" },
            { "CAPRSNC", "监狱" },
            { "CAPRSND", "监狱" },
            { "CAPRSNE", "监狱" },
            { "CAPRSNF", "监狱" },
            { "CAPRSNG", "监狱" },
            { "CASHUT", "储藏室" },
            { "CASLUMA", "贫民窟" },
            { "CASLUMB", "贫民窟" },
            { "CASLUMC", "贫民窟" },
            { "CASLUMD", "贫民窟" },
            { "CASLUME", "贫民窟" },
            { "CAETNT01", "帐篷" },
            { "CAETNT02", "帐篷" },
            { "CAETNT03", "帐篷" },
            { "CABUBB", "水桶" },
            { "CAWOODS", "木板" },
            { "CARODS01", "钢管" },
            { "CARODS02", "钢管" },
            { "CATPIP", "运输管输管道" },
            { "CAPIPE", "水泥管" },
            { "CALGHT01", "探照灯" },
            { "CAURB01", "电话亭" },
            { "CAURB02", "消防栓" },
            { "CAURB03", "好莱坞聚光灯" },
            { "CAWARE", "仓库" },
            { "CAWARE2", "仓库" },
            { "CADWAR", "仓库" },
            { "CADWARB", "仓库" },
            { "CAJUNK", "垃圾站" },
            { "CAFACC", "旧工厂" },
            { "CAFCTR", "工厂" },
            { "CAIND01", "工厂" },
            { "CAGARG", "车库" },
            { "CAHALL02", "会所" },
            { "CAWARS", "仓库" },
            { "CAWHSE", "仓库" },
            { "CANHSE01", "仓库" },
            { "CANHSE02", "仓库" },
            { "CABRKK", "铁棚屋" },
            { "CAJHUT", "竹屋" },
            { "CAJHUTB", "竹屋" },
            { "CASUPLA", "补给" },
            { "CABRLS", "补给" },
            { "CAWHEE", "水轮" },
            { "CACASK", "啤酒桶" },
            { "CAMRKT01", "集市" },
            { "CAMRKT02", "集市" },
            { "CAMRKT03", "补给" },
            { "CAMRKT04", "集市" },
            { "CAMRKT05", "集市" },
            { "CAMRKT06", "集市" },
            { "CAMRKT07", "集市" },
            { "CAMRKT08", "集市" },
            { "CALIT01N", "路灯" },
            { "CALIT01E", "路灯" },
            { "CALIT01S", "路灯" },
            { "CALIT0", "路灯" },
            { "CALIT01W", "路灯" },
            { "CALIT02L", "路灯" },
            { "CALIT02R", "路灯" },
            { "CALIT03N", "路灯" },
            { "CALIT03E", "路灯" },
            { "CALIT03S", "路灯" },
            { "CALIT03W", "路灯" },
            { "CAOPIP", "油管" },
          
            
    
               
           
           
        };

        /// <summary>
        /// 尝试获取单位/建筑/载具的中文名称
        /// </summary>
        /// <param name="technoName">单位/建筑/载具的英文名称</param>
        /// <param name="chineseName">输出的中文名称</param>
        /// <returns>是否找到对应的中文名称</returns>
        public static bool TryGetTechnoChineseName(string technoName, out string chineseName)
        {
            // 首先尝试从INI文件加载的翻译中查找
            if (TechnoNameManager.TryGetTechnoName(technoName, out chineseName))
                return true;
                
            // 如果INI文件中没有，则尝试从代码中硬编码的字典查找
            return TechnoChineseNameMap.TryGetValue(technoName, out chineseName);
        }

        public ObjectListPanel(WindowManager windowManager, EditorState editorState, Map map, TheaterGraphics theaterGraphics) : base(windowManager)
        {
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
        }

        protected EditorState EditorState { get; }
        protected Map Map { get; }
        protected TheaterGraphics TheaterGraphics { get; }

        public XNASuggestionTextBox SearchBox { get; private set; }
        public TreeView ObjectTreeView { get; private set; }

        private XNADropDown ddOwner;

        private bool IsChinese => TSMapEditor.UI.MainMenu.IsChinese;
        public void RefreshLanguage(bool isChinese)
        {
            // 刷新"所属方"标签
            if (Children.Count > 0 && Children[0] is XNALabel lblOwner)
                lblOwner.Text = isChinese ? "所属方：" : "Owner:";

            // 刷新下拉框
            ddOwner.Items.Clear();
            Map.GetHouses().ForEach(h => {
                string displayName;
                
                // 优先使用INI翻译文件中的翻译
                if (isChinese && CategoryNameManager.TryGetCategoryNameTranslation(h.ININame, out var iniTranslation))
                {
                    displayName = iniTranslation;
                }
                // 其次使用硬编码字典
                else if (CategoryNameMap.TryGetValue(h.ININame, out var names))
                {
                    displayName = isChinese ? names.zh : names.en;
                }
                else
                {
                    displayName = h.ININame;
                }
                
                ddOwner.AddItem(displayName, Helpers.GetHouseUITextColor(h));
            });
            ddOwner.SelectedIndex = Map.GetHouses().FindIndex(h => h == EditorState.ObjectOwner);

            // 刷新树分类和节点
            foreach (var category in ObjectTreeView.Categories)
            {
                // 优先使用INI翻译文件中的翻译
                if (isChinese && CategoryNameManager.TryGetCategoryNameTranslation(category.Text, out var iniTranslation))
                {
                    category.DisplayName = iniTranslation;
                }
                // 其次使用硬编码字典
                else if (CategoryNameMap.TryGetValue(category.Text, out var names))
                {
                    category.DisplayName = isChinese ? names.zh : names.en;
                }
                else
                {
                    category.DisplayName = category.Text;
                }

                // 递归刷新所有节点
                foreach (var node in category.Nodes)
                {
                    if (node.Tag is TechnoType techno)
                    {
                        string iniName = techno.ININame;
                        if (isChinese)
                        {
                            // 首先尝试从TechnoNameManager中获取翻译
                            if (TechnoNameManager.TryGetTechnoName(iniName, out var zhName))
                                node.DisplayName = zhName;
                            // 然后尝试从硬编码字典中获取
                            else if (TechnoChineseNameMap.TryGetValue(iniName, out zhName))
                                node.DisplayName = zhName;
                            else
                                node.DisplayName = techno.GetEditorDisplayName();
                        }
                        else
                            node.DisplayName = techno.GetEditorDisplayName();
                    }
                    else
                    {
                        node.DisplayName = node.Text;
                    }
                }
            }
            ObjectTreeView.RefreshScrollbar();
        }

        public override void Initialize()
        {
            var lblOwner = new XNALabel(WindowManager);
            lblOwner.Name = nameof(lblOwner);
            lblOwner.X = Constants.UIEmptySideSpace;
            lblOwner.Y = Constants.UIEmptyTopSpace;
            lblOwner.Text = IsChinese ? "所属方：" : "Owner:";
            AddChild(lblOwner);

            ddOwner = new XNADropDown(WindowManager);
            ddOwner.Name = nameof(ddOwner);
            ddOwner.X = lblOwner.Right + Constants.UIHorizontalSpacing;
            ddOwner.Y = lblOwner.Y - 1;
            ddOwner.Width = Width - Constants.UIEmptySideSpace - ddOwner.X;
            AddChild(ddOwner);
            ddOwner.SelectedIndexChanged += DdOwner_SelectedIndexChanged;

            SearchBox = new XNASuggestionTextBox(WindowManager);
            SearchBox.Name = nameof(SearchBox);
            SearchBox.X = Constants.UIEmptySideSpace;
            SearchBox.Y = ddOwner.Bottom + Constants.UIEmptyTopSpace;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            SearchBox.Height = Constants.UITextBoxHeight;
            SearchBox.Suggestion = "Search object... (CTRL + F)";
            AddChild(SearchBox);
            SearchBox.TextChanged += SearchBox_TextChanged;
            SearchBox.EnterPressed += SearchBox_EnterPressed;
            UIHelpers.AddSearchTipsBoxToControl(SearchBox);

            ObjectTreeView = new TreeView(WindowManager);
            ObjectTreeView.Name = nameof(ObjectTreeView);
            ObjectTreeView.Y = SearchBox.Bottom + Constants.UIVerticalSpacing;
            ObjectTreeView.Height = Height - ObjectTreeView.Y;
            ObjectTreeView.Width = Width;
            AddChild(ObjectTreeView);
            ObjectTreeView.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 222), 2, 2);

            ObjectTreeView.SelectedItemChanged += ObjectTreeView_SelectedItemChanged;
            EditorState.ObjectOwnerChanged += EditorState_ObjectOwnerChanged;

            base.Initialize();

            RefreshHouseList();

            Parent.ClientRectangleUpdated += Parent_ClientRectangleUpdated;
            RefreshPanelSize();

            InitObjects();

            KeyboardCommands.Instance.NextSidebarNode.Triggered += NextSidebarNode_Triggered;
            KeyboardCommands.Instance.PreviousSidebarNode.Triggered += PreviousSidebarNode_Triggered;

            Map.HousesChanged += (s, e) => RefreshHouseList();
            Map.HouseColorChanged += (s, e) => RefreshHouseList();

            // 确保初次启动时所有条目都按当前语言刷新显示
            RefreshLanguage(IsChinese);
        }

        private void NextSidebarNode_Triggered(object sender, EventArgs e)
        {
            if (Enabled)
                ObjectTreeView.SelectNextNode();
        }

        private void PreviousSidebarNode_Triggered(object sender, EventArgs e)
        {
            if (Enabled)
                ObjectTreeView.SelectPreviousNode();
        }

        private void ObjectTreeView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (EditorState.ObjectOwner == null)
                return;

            if (ObjectTreeView.SelectedNode != null)
                ObjectSelected();
        }

        protected abstract void ObjectSelected();

        private void SearchBox_EnterPressed(object sender, EventArgs e)
        {
            ObjectTreeView.FindNode(SearchBox.Text, true);
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text) || SearchBox.Text == SearchBox.Suggestion)
                return;

            ObjectTreeView.FindNode(SearchBox.Text, false);
        }

        protected abstract void InitObjects();

        protected (Texture2D regular, Texture2D remap) GetTextureForVoxel<T>(T objectType, VoxelModel[] typeGraphicsArray, RenderTarget2D renderTarget, byte facing) where T : TechnoType, IArtConfigContainer
        {
            var voxelModel = typeGraphicsArray[objectType.Index];

            if (voxelModel == null)
                return (null, null);

            Renderer.BeginDraw();

            var frame = voxelModel.GetFrame(facing, RampType.None, false);
            if (frame == null || frame.Texture == null)
            {
                Renderer.EndDraw();
                return (null, null);
            }

            var remapFrame = voxelModel.GetRemapFrame(facing, RampType.None, false);

            Renderer.EndDraw();

            // Render them as smaller textures to be independent of the voxel cache
            Texture2D regularTexture = Helpers.RenderTextureAsSmaller(frame.Texture, renderTarget, GraphicsDevice);
            Texture2D remapTexture = null;

            if (remapFrame != null && remapFrame.Texture != null)
                remapTexture = Helpers.RenderTextureAsSmaller(remapFrame.Texture, renderTarget, GraphicsDevice);

            return (regularTexture, remapTexture);
        }

        protected virtual (Texture2D regular, Texture2D remap) GetObjectTextures<T>(T objectType, ShapeImage[] textures) where T : TechnoType, IArtConfigContainer
        {
            Texture2D texture = null;
            Texture2D remapTexture = null;
            if (textures != null)
            {
                if (textures[objectType.Index] != null)
                {
                    int frameCount = textures[objectType.Index].GetFrameCount();

                    for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                    {
                        var frame = textures[objectType.Index].GetFrame(frameIndex);
                        if (frame != null)
                        {
                            texture = textures[objectType.Index].GetTextureForFrame_RGBA(frameIndex);
                            if (objectType.GetArtConfig().Remapable && textures[objectType.Index].HasRemapFrames())
                                remapTexture = textures[objectType.Index].GetRemapTextureForFrame_RGBA(frameIndex);
                            break;
                        }
                    }
                }
            }

            return (texture, remapTexture);
        }

        protected void InitObjectsBase<T>(List<T> objectTypeList, ShapeImage[] textures, Func<T, bool> filter = null) where T : TechnoType, IArtConfigContainer
        {
            var sideCategories = new List<TreeViewCategory>();
            for (int i = 0; i < objectTypeList.Count; i++)
            {
                var objectType = objectTypeList[i];

                if (!objectType.EditorVisible)
                    continue;

                if (!objectType.IsValidForTheater(Map.LoadedTheaterName))
                    continue;

                if (filter != null && !filter(objectType))
                    continue;

                List<ObjectCategory> categories = new List<ObjectCategory>(1);

                string categoriesString = objectType.EditorCategory;

                if (categoriesString == null)
                    categoriesString = objectType.Owner;

                if (string.IsNullOrWhiteSpace(categoriesString))
                {
                    categories.Add(new ObjectCategory("Uncategorized", Color.White));
                }
                else
                {
                    string[] owners = categoriesString.Split(',');

                    for (int ownerIndex = 0; ownerIndex < owners.Length; ownerIndex++)
                    {
                        Color remapColor = Color.White;

                        string ownerName = owners[ownerIndex];
                        ownerName = Map.EditorConfig.EditorRulesIni.GetStringValue("ObjectOwnerOverrides", ownerName, ownerName);

                        House house = Map.StandardHouses.Find(h => h.ININame == ownerName);
                        if (house != null)
                        {
                            int actsLike = house.ActsLike.GetValueOrDefault(-1);
                            if (actsLike > -1)
                                ownerName = Map.StandardHouses[actsLike].ININame;
                        }

                        House ownerHouse = Map.StandardHouses.Find(h => h.ININame == ownerName);
                        if (ownerHouse != null)
                        {
                            remapColor = ownerHouse.XNAColor;
                        }
                        else
                        {
                            // As as last resort, check if EditorRules has a remap color specified for the side
                            string colorOverrideName = Map.EditorConfig.EditorRulesIni.GetStringValue("ObjectOwnerColors", ownerName, null);
                            if (!string.IsNullOrWhiteSpace(colorOverrideName))
                            {
                                var rulesColor = Map.Rules.Colors.Find(c => c.Name == colorOverrideName);
                                if (rulesColor != null)
                                    remapColor = rulesColor.XNAColor;
                            }
                        }

                        // Prevent duplicates that can occur due to category overrides
                        if (!categories.Exists(c => c.Name == ownerName))
                        {
                            categories.Add(new ObjectCategory(ownerName, remapColor));
                        }
                    }
                }

                var extractedTextures = GetObjectTextures(objectType, textures);

                categories = categories.OrderBy(c => Map.EditorConfig.EditorRulesIni.GetIntValue("ObjectCategoryPriorities", c.Name, 0)).ToList();

                for (int categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
                {
                    var category = FindOrMakeCategory(categories[categoryIndex].Name, sideCategories);

                    category.Nodes.Add(new TreeViewNode()
                    {
                        Text = objectType.ININame,
                        Texture = extractedTextures.regular,
                        RemapTexture = extractedTextures.remap,
                        RemapColor = categories[categoryIndex].RemapColor,
                        Tag = objectType
                    });
                }
            }

            for (int i = 0; i < sideCategories.Count; i++)
                sideCategories[i].Nodes = sideCategories[i].Nodes.OrderBy(n => n.Text).ToList();

            sideCategories = sideCategories.OrderBy(c => Map.EditorConfig.EditorRulesIni.GetIntValue("ObjectCategoryPriorities", c.Text, int.MaxValue)).ToList();
            sideCategories.ForEach(c => ObjectTreeView.AddCategory(c));
        }

        private TreeViewCategory FindOrMakeCategory(string categoryName, List<TreeViewCategory> categoryList)
        {
            var category = categoryList.Find(c => c.Text == categoryName);
            if (category != null)
                return category;

            category = new TreeViewCategory() { Text = categoryName };
            category.DisplayName = CategoryNameMap.TryGetValue(categoryName, out var names)
                ? (IsChinese ? names.zh : names.en)
                : categoryName;
            categoryList.Add(category);
            return category;
        }

        private void Parent_ClientRectangleUpdated(object sender, EventArgs e)
        {
            Height = Parent.Height - Y;
            ObjectTreeView.Height = Height - ObjectTreeView.Y;
            RefreshSize();
        }

        private void RefreshPanelSize()
        {
            Width = Parent.Width;
            ddOwner.Width = Width - Constants.UIEmptySideSpace - ddOwner.X;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            ObjectTreeView.Width = Width;
        }

        private void DdOwner_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 添加边界检查，避免索引越界异常
            if (ddOwner.SelectedIndex >= 0 && ddOwner.SelectedIndex < Map.GetHouses().Count)
            {
                EditorState.ObjectOwner = Map.GetHouses()[ddOwner.SelectedIndex];
            }
        }

        private void RefreshHouseList()
        {
            // 先移除事件监听器，防止在修改UI过程中触发事件
            ddOwner.SelectedIndexChanged -= DdOwner_SelectedIndexChanged;
            
            ddOwner.Items.Clear();
            Map.GetHouses().ForEach(h =>
            {
                string displayName;
                
                // 优先使用INI翻译文件中的翻译
                if (IsChinese && CategoryNameManager.TryGetCategoryNameTranslation(h.ININame, out var iniTranslation))
                {
                    displayName = iniTranslation;
                }
                // 其次使用硬编码字典
                else if (CategoryNameMap.TryGetValue(h.ININame, out var names))
                {
                    displayName = IsChinese ? names.zh : names.en;
                }
                else
                {
                    displayName = h.ININame;
                }
                
                ddOwner.AddItem(displayName, Helpers.GetHouseUITextColor(h));
            });
            
            // 确保选中索引有效
            int selectedIndex = Map.GetHouses().FindIndex(h => h == EditorState.ObjectOwner);
            if (selectedIndex >= 0 && selectedIndex < ddOwner.Items.Count)
            {
                ddOwner.SelectedIndex = selectedIndex;
            }
            else if (ddOwner.Items.Count > 0)
            {
                // 如果当前ObjectOwner无效，选择第一个项目
                ddOwner.SelectedIndex = 0;
                if (Map.GetHouses().Count > 0)
                {
                    EditorState.ObjectOwner = Map.GetHouses()[0];
                }
            }

            // 重新添加事件监听器
            ddOwner.SelectedIndexChanged += DdOwner_SelectedIndexChanged;
        }

        private void EditorState_ObjectOwnerChanged(object sender, EventArgs e)
        {
            ddOwner.SelectedIndexChanged -= DdOwner_SelectedIndexChanged;

            // 添加边界检查
            int newIndex = Map.GetHouses().FindIndex(h => h == EditorState.ObjectOwner);
            if (newIndex >= 0 && newIndex < ddOwner.Items.Count)
            {
                ddOwner.SelectedIndex = newIndex;
            }
            else if (ddOwner.Items.Count > 0)
            {
                // 如果找不到匹配的阵营，选择第一个
                ddOwner.SelectedIndex = 0;
            }

            ddOwner.SelectedIndexChanged += DdOwner_SelectedIndexChanged;
        }
    }
}
