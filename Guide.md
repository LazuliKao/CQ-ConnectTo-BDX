## 参数
- 配置文件位置../app/ml.paradis.tool/config.json
## 配置文件使用方法
- 很多内容参考了MCBE行为包的自定义方法，还原度不高但自定义程度很高
- 如果你懂得如何编写行为包，那么这个自定义配置上手会很容易
```json
{
    "Servers": [
        {
            "Address": "ws://localhost:29132/mc",
            "Token": "76A2173BE6393254E72FFA4D6DF1030A",
            "Tag": "服务器1",
            "Triggers": [
               
                {
                    "Variants": [
                        //
                        //{
                        //    "Name": "Message",
                        //     //自定义名称
                        //    "Path": ["body","properties","Message"
                        //     //数值源数据包目录，参考...
                        //    ]
                        //}
                        {
                            "Name": "PlayerName",
                            "Path": [
                                "target"
                            ]
                        },
                        {
                            "Name": "Message",
                            "Path": [
                                "text"
                            ]
                        }
                    ],
                    "Operations": [
                        //"Operations"操作，
                        //  会在下面的筛选器和Actions被执行前执行
                        //  同样的，每个操作都有一个"Filter"来筛选是否执行，"Filter":true保持执行
                        //  可选参数"CreateVariant"，即创建变量来储存，否则直接赋值给"TargetVariant"
                        //-------------------------------------------------
                        //普通替换
                        //{
                        //    "Type": "Replace",          //操作类型
                        //    "TargetVariant": "Message",//操作的变量，需要提前在"Variants"定义
                        //    "Find": "***",              //寻找的字符串
                        //    "Replacement": "???",       //替换掉的字符串
                        //    "Filter": true              //筛选是否执行 保持执行请填true
                        //}
                        //--------------------------------------------------
                        //正则表达式替换
                        //{
                        //    "Type": "RegexReplace",     //操作类型
                        //    "TargetVariant": "Message",//操作的变量，需要提前在"Variants"定义
                        //    "Pattern": "^!",            //正则表达式匹配
                        //    "Replacement": "§c",        //替换掉的字符串
                        //    "Filter":true               //筛选是否执行 保持执行请填true
                        //} 
                        //--------------------------------------------------
                        //正则表达式子表达式筛选
                        //{
                        //    "Type": "RegexGet",     //操作类型
                        //    "TargetVariant": "Message",//操作的变量，需要提前在"Variants"定义
                        //    "Pattern": "(?<MyValue>23+)",         //正则表达式匹配 基于C#的高端正不懂勿用
                        //    "GroupName":"MyValue",            //筛选组名，需要上方 Pattern有定义，否则返回空
                        //    "Filter":true              //筛选是否执行 保持执行请填true
                        //} 
                        //--------------------------------------------------
                        //字符串格式化(变量替入)
                        //{
                        //    "Type": "Format",                         //操作类型
                        //    "CreateVariant": "ReturnMessage",         //创建的变量，不需要提前在"Variants"定义，自动创建用来储存返回值
                        //    "Text": "%PlayerName%发送了消息:%Message%" //格式化的字符串
                        //    "Filter":true                             //筛选是否执行 保持执行请填true
                        //} 
                        //--------------------------------------------------
                        //转Unicode
                        //{
                        //    "Type": "ToUnicode",                      //操作类型
                        //    "CreateVariant": "ReturnMessage",         //创建的变量，不需要提前在"Variants"定义，自动创建用来储存返回值
                        //    "Text": "%PlayerName%发送了消息:%Message%" //格式化的字符串
                        //    "Filter":true                             //筛选是否执行 保持执行请填true
                        //} 
                        //
                        
                    ],
                    "Filter": {
                        "all_of": [
                            {
                                "Path": [
                                    "operate"
                                ],
                                "Operator": "==",
                                "Value": "onmsg"
                            }
                        ]
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
                    },
                    "Actions": [
                        {
                            "Target": "log",
                            "Debug": "玩家%PlayerName%发送了消息:%Message%"
                        },
                        {
                            "Target": "sender",
                            "cmd": "say §e§'<复读机§'>§b%PlayerName%§a=>§r%Message%"
                        },
                        {
                            "Target": "QQGroup",
                            "groupID": 386475891
                        }
                        //Target可用参数:
                        //
                        //"log"=>酷Q软件添加日志,
                        //---配套参数>"info":"输出日志"
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
                    ]
                }
            ]
        }
    ],
    "Groups": [
        {
            "ID": 386475891,
            "Triggers": [
                {}
            ]
        }
    ]
}
```
# WebSocketAPI
## 玩家消息(服务端发出
```json
{"operate":"onmsg","target":"WangYneos","text":"HelloWorld"}
//操作标识——————————目标——————————————————返回信息（玩家聊天内容）
```
## 玩家加入(服务端发出
```json
{"operate":"onjoin","target":"WangYneos","text":"Joined server"}
//操作标识——————————---目标——————————————————返回信息（加入了服务器）
```
## 玩家退出(服务端发出
```json
{"operate":"onleft","target":"WangYneos","text":"Lefted server"}
//与上面类似
```
## 玩家使用命令(服务端发出
```json
{"operate":"onCMD","target":"WangYneos","CMD":"/list"}
//操作标识-----------目标玩家--------------执行的命令
```
## WS客户端使用命令
>发送
```json
{"op":"runcmd","passwd":"CD92DDCEBFB8D3FB1913073783FAC0A1","cmd":"kick WangYneos"}
//标识--操作类型--密码---------------------------------------执行内容----------------
```
>服务端返回
```json
{"operate":"runcmd","Auth":"PasswdMatch","feedback":"commands.kick.success"}
//操作标识---操作类型--密码验证--成功---------返回内容----------------------------
{"operate":"runcmd","onError":"Auth","text":"Password Not Match"}
//操作标识---操作类型--出错-------验证---------返回内容--------------
{"operate":"onCMD","target":"gxh2004","CMD":"/kick gxh2004"}
```
## 密码获得规则
服务端获取密码
+当前 年月日时分
（无分号，空格
>例如密码是passwd
>则验证密码passwd202004062016
>（2020年4月6日8点16
取MD5（大写
>得到CD92DDCEBFB8D3FB1913073783FAC0A1
客户端与服务端一致则验证成功

