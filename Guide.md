## <h2>参数</h2>

* 配置文件位置../app/ml.paradis.tool/config.json

----

## 入门(坟)

#### 首次使用直接打开酷Q插件菜单设置就行了

* 更多高级内容还是要参考下面的文档！！！
* ~~1. 插件载入酷Q，运行，然后打开自动释放于../app/ml.paradis.tool/config.json的默认配置文件（建议使用Visual Studio Code编辑，语言选择JSON with Comments）~~
* ~~2. Ctrl+H打开替换工具，把所有默认的测试群号替换成你的群号~~
* ~~3. 编辑服务器配置Servers下的JSON数组，将Address与BDX-Websocket端对应~~
* ~~4. 再次Ctrl+H把所有"测试服务器1"替换成你想修改的名称~~
* ~~5. 投入使用(体验bug)~~

----

# 配置文件使用方法

* 很多内容低仿了MCBE行为包的自定义方法，还原度不高但自定义程度很高
* 如果你懂得如何编写行为包，那么这个自定义配置上手会很容易
* ## 配置文件基本框架

---
| JSON内容 |注释| 快捷目录 |
|----|----|----|
| `{` |||
|&#8195; `"configVersion": 0.3,` | `//配置文件版本标记，请不要随意改动` | |
|&#8195; `"Servers": [{...},{...},...],` | `//[{服务器1},{服务器2},······]无上限` |[WS服务器配置模板](#WS服务器配置)|
|&#8195; `"CheckConnectionTime": 60,` | `//检查连接时长，如果服务器掉线了会在检查后自动重连，单位:秒` ||
|&#8195; `"Groups": [{...},{...},...],` | `//[{群聊1},{群聊2},······]无上限(每个群聊单独配置)` |[群聊配置模板](#群聊配置)|
|&#8195; `"Timers": [{...},{...},...],` | `//` |[计时器](#计时器)|
|&#8195; `"Tasks": [{...},{...},...]` | `//` |[定时任务](#定时任务)|
| `}` ||
---

* ## WS服务器配置

| JSON文本 | 备注 |
|----|----|
| `{` ||
|&#8195; `"Address": "ws://localhost:29132/mc",` ||
|&#8195; `"Passwd": "passwd",` ||
|&#8195; `"Tag": "服务器1",` ||
|&#8195; `"Actions": [{...},{...},...]` |//[通用动作](#通用动作)|
| `}` ||

----

* ## 群聊配置

| JSON文本 | 备注 |
|----|----|
| `{` ||
|&#8195; `"ID": 386475891,` |//群号码|
|&#8195; `"SendToServer": true,` |//是否转发到服务器|
|&#8195; "SendToServerRegex": "^(?!/)(.+&#124; \\\\s+)+", |//正则筛选，符合条件会转发匹配到的内容<br>//---------Examples---------<br>//群聊消息>无条件转发，可以这么填（或者把这行删了）<br>//"SendToServerRegex": "(.*&#124; \\\\s*)*", <br>//群聊消息>不转发以"/"开头的消息，可以这么填(使用了"零宽度负预测先行断言(?!exp)")<br>//"SendToServerRegex": "^(?!/)(.+&#124; \\\\s+)+", <br>//群聊消息>指定以"+"开头的消息，并转发+后面的内容，可以这么填(使用了"零宽度正回顾后发断言(?<=exp)")<br>//"SendToServerRegex": "(?<=^\+)(.+&#124; \\\\s+)+", <br>//推荐正则教程(如果你不会改):https://deerchao.cn/tutorials/regex/regex.htm
|&#8195; `"SendToServerFormat": {` <br>&#8195; &#8195; `"CQAt": "§r§l§6@§r§6{0}§a",` <br>&#8195; &#8195; `"CQImage": "§r§l§d[图骗]§r§a",` <br>&#8195; &#8195; `"CQEmoji": "§r§l§d[emoji]§r§a",` <br>&#8195; &#8195; `"CQFace": "§r§l§c[表情]§r§a",` <br>&#8195; &#8195; `"CQBface": "§r§l§d[大表情:§r§o§7{0}§r§l§d]§r§a",` <br>&#8195; &#8195; `"Main": "§b【群聊消息】§e<{0}>§a{1}"},` | `//[CQ:xx]自定义转换格式,注意:{n}是参数变量，不是每个都有的` |
|&#8195; `"Actions": [{...},{...},...]` |//[通用动作](#通用动作)|
| `}` ||
----

* ## 通用动作

| JSON文本 | 备注 |
|----|----|
| `{` ||
|&#8195; `"Type": "xxxx",` |//[通用自定义动作](#通用自定义动作)
|&#8195; `"Parameters": {"xxx":"xxx"},` |//[通用自定义动作的参数](#通用自定义动作)
| `}` ||

> [接收的数据包示例](#数据包示例)

* - ### 通用自定义动作
  + <h3>> "Actions" <</h3>

> #### 自定义操作:
> 依次执行, 
> <br>每个操作都可以有一个"Filter":{...}来筛选是否执行，"Filter":true或者无"Filter"则总是执行, 
> <br>部分操作(比如replace)有可选参数"CreateVariant"，即创建变量来储存，否则直接赋值给"TargetVariant", 
>
> ----
> - <h3>自定义操作: 创建变量(通过收到的消息的json层级目录)</h3>
>``` jsonc
>{
>   "Type": "var",
>   "Parameters": {
>      "Name": "MsgType",
>      "Path": ["operate"]//接收的json层级目录
>                         //或去掉Path使用"ServerConfig":["xxx"]
>   }
>}
>```
> ----
> - <h3>自定义操作: 创建变量(调用接收方的配置文件(本文件)json层级目录)</h3>
>``` jsonc
>{
>   "Type": "var",
>   "Parameters": {
>      "Name": "MsgType",
>      "ServerConfig":["Address"]
>   }
>}
>```
> ----
> - <h3>自定义操作: 执行子Actions</h3>
>``` jsonc
>{
>   "Type": "DoActions",
>   "Filter":true,
>   "Parameters": {
>      "Actions":[{...},{...}]//套娃
>   }
>}
>```
> ----
> - <h3>自定义操作: 普通替换</h3>
>``` jsonc
>{
>    "Type": "Replace",              //操作类型
>    "Filter": true,                 //筛选是否执行 保持执行请填true
>    "Parameters": {
>        "TargetVariant": "Message", //操作的变量，需要提前在"Variants"定义
>        "Find": "***",              //寻找的字符串
>        "Replacement": "???"        //替换掉的字符串
>   }
>}
>```
> ----
> - <h3>自定义操作: 正则表达式替换</h3>
>``` jsonc
>{
>    "Type": "RegexReplace",     //操作类型
>    "Filter":true,               //筛选是否执行 保持执行请填true或注释掉
>    "Parameters": {
>        "TargetVariant": "Message", //操作的变量，需要提前在"Variants"定义
>        "Pattern": "^!",            //正则表达式匹配
>        "Replacement": "§c"        //替换掉的字符串
>   }
>} 
>```
> ----
> - <h3>自定义操作: 正则表达式子表达式筛选</h3>
>``` jsonc
>{
>    "Type": "RegexGet",          //操作类型
>    "Filter":true,                //筛选是否执行 保持执行请填true或注释掉
>    "Parameters": {
>        "TargetVariant": "Message",  //操作的变量，需要提前在"Variants"定义
>        "Pattern": "(?<MyValue>23+)",//正则表达式匹配 基于C#的高端正不懂勿用
>        "GroupName":"MyValue"       //筛选组名，需要上方 Pattern有定义，否则返回空
>    }
>} 
>```
> ----
> - <h3>自定义操作: 字符串格式化(变量替入)</h3>
>``` jsonc
>{
>    "Type": "Format",                         //操作类型
>    "Filter":true,                //筛选是否执行 保持执行请填true或注释掉
>    "Parameters": {
>        "CreateVariant": "ReturnMessage",         //创建的变量，不需要提前在"Variants"定义，自动创建用来储存返回值
>        "Text": "%PlayerName%发送了消息:%Message%"//格式化的字符串
>   }
>}
>```
> ----
> - <h3>自定义操作: 转Unicode</h3>
>``` jsonc
>{
>    "Type": "ToUnicode",                      //操作类型
>    "Filter":true,                             //筛选是否执行 保持执行请填true
>    "Parameters": {
>        "CreateVariant": "ReturnMessage",         //创建的变量，不需要提前在"Variants"定义，自动创建用来储存返回值
>        "Text": "%PlayerName%发送了消息:%Message%"//格式化的字符串
>    }
>}
>```
> ----
> - <h3>自定义操作: MotdBE查服(通过地址和端口)</h3>
> - - 纯C#: System. Net. Sockets直接向基岩版服务器发送查询请求，没用别的接口，一般不会失效
> - - 如果你想用网页的接口请参考下面的HTML获取
>``` jsonc
>{
>    "Type": "MotdBE",                //操作类型
>    "Filter":true,                     //筛选是否执行 保持执行请填true
>    "Parameters": {
>        "CreateVariant": "MotdResult",   //创建的变量，不需要提前在"Variants"定义，自动创建用来储存返回值
>        "IP": "1.2.3.4",                 //查询的服务器地址（可以引用变量，比如%ip%）
>        "PORT": "19132",                 //查询的服务器端口（可以引用变量，比如%ip%）
>        "Text":"查询成功:\n名称$$",
>        //返回变量列表(使用"$名称$"引用，而不是%)
>        //  { "type" ,  get[InfoList.type] },
>        //{ "description" ,  get[InfoList.description] },
>        //{ "connectionVer" ,  get[InfoList.connectionVer] },
>        //{ "gameVer" ,  get[InfoList.gameVer] },
>        //{ "onlineplayers" ,  get[InfoList.onlineplayers] },
>        //{ "maxPlayers" ,  get[InfoList.maxPlayers] },
>        //{ "serverUID" ,  get[InfoList.serverUID] },
>        //{ "defaultMode" ,  get[InfoList.defaultMode] },
>        //{ "isBDS" ,  get[InfoList.isBDS] },
>        //{ "port" ,  get[InfoList.port] },
>        //{ "portv6" ,  get[InfoList.portv6] }, 
>        "FailedText":"查询失败:{0}"      //无法连接至世界！
>    }
>}
>```
> ----
> - <h3>自定义操作: 获取网页HTML</h3>
>``` jsonc
>{
>    "Type": "GetHTML",              //操作类型
>    "Filter":true,                   //筛选是否执行 保持执行请填true
>    "Parameters": {
>        "CreateVariant": "HTMLstr",     //创建的变量，不需要提前在"Variants"定义，自动创建用来储存返回值
>        "URI": "https://www.baidu.com/"//网址(Api地址)
>   }
>}
>```
> ----
> - <h3>自定义操作: 延时</h3>
>``` jsonc
>{
>    "Type": "Sleep", //操作类型
>    "Filter":true,    //筛选是否执行 保持执行请填true
>    "Parameters": {
>        "Time":2333     //延时时间，单位毫秒
>   }
>} 
>```
> ----
> - <h3>自定义操作: 写配置项(.ini)</h3>
>``` jsonc
>{
>    "Type": "write_ini", //操作类型
>    "Filter":true,    //筛选是否执行 保持执行请填true
>    "Parameters": {
>        "Path":"~/test.ini",//配置文件目录,"~"开头表示插件文件夹路径
>                   //注意：路径分隔符用"/",而不是"\\"或"\"!!!
>        "Section",//配置项写入的节点
>        "Key":"", //配置项写入的标识
>        "Value":""//配置项写入的值
>   }
>} 
>```
> ----
> - <h3>自定义操作: 读配置项(.ini)</h3>
>``` jsonc
>{
>    "Type": "read_ini", //操作类型   
>    "Filter":true,   //筛选是否执行 保持执行请填true
>    "Parameters": {
>        "Path":"~/test.ini",//配置文件目录,"~"开头表示插件文件夹路径
>                   //注意：路径分隔符用"/",而不是"\\"或"\"!!!
>        "Section",//配置项写入的节点
>        "Key":"", //配置项写入的标识
>        "CreateVariant":""//将读取到的Value写入变量
>   }
>} 
>```
> ----
> - <h3>自定义操作: 输出日志(.log)</h3>
>``` jsonc
>{
>    "Type": "write_log", //操作类型
>    "Parameters": {
>        "Path":"~/test.log",//配置文件目录,"~"开头表示插件文件夹路径
>                   //注意：路径分隔符用"/",而不是"\\"或"\"!!!
>        "WriteLine":""//log输出内容
>   }
>} 
>```
> ----
> - <h3>自定义操作: 发送服务器命令</h3>
>``` jsonc
>{
>    "Type": "servers", //操作类型
>    "Parameters": {
>        "cmd": "%CMDStr%",//指令内容
>        "Filter":true,    //筛选判断
>        "CallbackActions":[]//收到反馈时执行的Actions
>   }
>} 
>```
> ----
> - <h3>自定义操作: 发送私聊消息（好友）</h3>
>``` jsonc
>{
>    "Type": "PrivateMessage",
>    "Parameters": {
>        "QQ": 441870948,
>        "Message":"Hello?"
>   }
>} 
>```
> ----
> - <h3>自定义操作: 发送群消息</h3>
>``` jsonc
>{
>    "Type": "GroupMessage",
>    "Parameters": {
>        "GroupID": 441870948,
>        "Message":"Hello?"
>   }
>} 
>```
> ----

* - ### 自定义条件筛选器
  + <h3>> "Filter" <</h3>
``` jsonp
//"Filter"筛选器
//  只有条件返回结果为true才会执行操作(Actions)
//-------------------------------------------------
//可用参数"all_of":[{xxx1},{xxx2}]满足所有子条件才返回true
//可用参数"any_of":[{xxx1},{xxx2}]满足任一子条件就返回true
//支持无限层数嵌套,比如:
//"Filter": {
// "all_of": [ 
// { "Path": [ "body", "properties", "MessageType" ],
//  "Operator": "==",
//  "Value": "chat"},
//  {"any_of":[{"all_of":[true,false]},false,true]}
// ]
//}
//-------------------------------------------------
//条件比较器
//{
//  "Path": ["body","properties","MessageType"],
//     //数值源数据包目录，参考github
//  "Operator": "==",
//      //比较的操作,可选：
//      //"==" "!=" "is" "not" //文字或数值
//      //">" "<" ">=" "<="    //如果为无法转化为数值的文字会直接返回false
//  "Value": "chat"
//      //比较的目标值
//}
//--------------------------------------------------
//变量比较器
//{
//  "Variant": "PlayerName",
//      //注意：需要提前在上边的"Variants"标签下面定义，否则返回控制台返回报错信息
//  "Operator": "==",
//      //比较的操作,可选：
//      //"==" "!=" "is" "not" //文字或数值
//      //">" "<" ">=" "<="    //如果为无法转化为数值的文字会直接返回false
//  "Value": "chat"
//      //比较的目标值
//}
```

* - ### 触发器自定义动作
  + <h3>> "Actions" <</h3>

``` jsonc
{
    "Target": "log",
    "Parameters": {
       "Debug": "玩家%PlayerName%发送了消息:%Message%"}
},
{
    "Target": "sender",
    "Parameters": {
       "cmd": "say §e§'<复读机§'>§b%PlayerName%§a=>§r%Message%"
    }
},
{
    "Target": "QQGroup",
    "Parameters": {
       "groupID": 386475891
    }
}
//Target可用参数:
//
//"log"=>酷Q软件添加日志,
//---配套参数>"Info":"输出日志"
           //"Debug":"输出日志"
//
//"sender"=>向这条消息触发的发送者(服务器)发送消息(命令)
//"other"=>向这条消息触发的发送者以外的连接的(服务器)发送消息(命令)
//"all"=>所有连接的服务器
//---配套参数>"cmd":"发送的命令"
//
//"QQGroup"=>向对应的qq群发送消息
//---配套参数>"groupID":群号码  (<= long)
//                     (群号码必须是机器人已添加的群，否则会报错)
//
//"doTriggers":[{...},{...}]
//可以嵌套上面的Trigger，支持无限套娃,注意:收到的消息会沿用...
//
//%变量名%可以引用变量，注意：需要提前在上边的"Variants"标签下面定义，否则返回null

```

``` jsonc
//来自QQ群时
未写完，记得催我
log
servers
QQGroup
ReturnGroupMessageAtFrom

ReturnPrivateMessage
--- "QQ":xxx

 
```

----

* ## 详细示例

``` jsonc
待更新......(咕咕咕)
```

----

## 云黑Api示例

> 把下面这一段复制到

``` jsonc
待更新......(咕咕咕)
```

``` jsonc
待更新......(咕咕咕)
```

----

* ## 数据包示例
  + ## 群聊消息接收

    >### 获取到的消息目录
    ><hr>
    >
    >``` 
    >{
    >  "Message": "23333",
    >  "FromQQ": 441870948,
    >  "FromQQNick": "g???x???h???",
    >  "FromGroup": 386475891,
    >  "IsFromAnonymous": false,
    >  "Id": 2,
    >  "MemberInfo": {
    >    "Card": "gxh2004",
    >    "Sex": 0,
    >    "Age": 16,
    >    "Area": "杭州",
    >    "JoinGroupDateTime": "2017-05-13T22:38:10+08:00",
    >    "LastSpeakDateTime": "2020-04-07T14:00:43+08:00",
    >    "Level": "吐槽",
    >    "MemberType": "Creator",
    >    "ExclusiveTitle": "",
    >    "ExclusiveTitleExpirationTime": "1970-01-01T08:00:00+08:00"
    >  },
    >  "GroupInfo": {
    >    "Name": "机器人测试",
    >    "CurrentMemberCount": 7,
    >    "MaxMemberCount": 200
    >  }
    >}
    >```

  ## WebSocketAPI收包目录
    >  ### 玩家消息(服务端发出
    >  ### player send a message
    >
    >``` json
    >  {"operate":"onmsg","target":"WangYneos","text":"HelloWorld"}
    >  //操作标识——————————目标——————————————————返回信息（玩家聊天内容）
    >  ```
    >  ### 玩家加入(服务端发出
    >  ### when a playe join the server
    >  
    >
    >``` json
    >  {"operate":"onjoin","target":"WangYneos","text":"target's ip address"}
    >  //操作标识——————————---目标——————————————————返回信息（玩家ip）
    >  ```
    >
    >  ### 玩家退出(服务端发出
    >
    >    ### when the player left the server
    >    
    >
    >``` json
    >    {"operate":"onleft","target":"WangYneos","text":"Lefted server"}
    >    //与上面类似
    >    ```
    >
    >    ### 玩家使用命令(服务端发出
    >    ### when the player use a command
    >    
    >
    >``` json
    >    {"operate":"onCMD","target":"WangYneos","text":"/list"}
    >    //操作标识-----------目标玩家--------------执行的命令
    >    ```
    >
    >    ### WS客户端使用命令
    >    ### WebSocket Client execute a command
    >    服务端返回
    >    feedback by server
    >``` json
    >    {"operate":"runcmd","Auth":"PasswdMatch","text":"Command Feedback"}
    >    //操作标识---操作类型--密码验证--成功---------返回内容----------------------------
    >    {"operate":"runcmd","Auth":"Failed","text":"Password Not Match" }
    >    //操作标识---操作类型--出错-------验证---------返回内容--------------
    >    ```
  