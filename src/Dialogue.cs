namespace DragonCompanion {
public static class Dialogue {
 public static readonly string[] Keys={"tsundere","cute","gentle","concise"};
 public static readonly string[] Names={"傲娇","可爱","温柔","简洁"};
 public static string Reply(string style,string intent,int variant=0){int index=System.Array.IndexOf(Keys,style);if(index<0)index=0;string[] lines;
  switch(intent){
   case "hello":lines=new[]{"我在呢。才没有特意等你……有什么事就说吧。","GPT 到啦～今天也一起加油呀！","我在。慢慢来，我们一件一件完成。","我在。可以查看额度、设置番茄钟或休息。"};break;
   case "thanks":lines=new[]{"哼，收到啦。下一件事也一起做好吧。","收到夸夸！尾巴都忍不住摇起来啦～","不用客气。能帮到你就好。","不客气。"};break;
   case "rest":lines=new[]{"好吧，我也歇一会儿。你可别又偷偷熬夜。","休息时间到～我抱着尾巴陪你歇一会儿。","好，先放松一下。照顾好自己，事情可以稍后继续。","进入休息状态。"};break;
   case "pat":lines=variant%2==0?new[]{"唔，龙角可不是天线。轻一点啦。","摸摸头收到～今天的开心加一！","我在这里，陪你慢慢把事情做好。","收到互动。"}:new[]{"就、就再摸一下好了。别得意。","尾巴摇摇～GPT 很开心见到你！","辛苦了，停下来缓口气也很好。","任务提醒已就绪。"};break;
   default:lines=new[]{"我现在会的口令是：你好、谢谢、额度、番茄钟、日程、休息。复杂问题还是回 Codex 问吧。","我听着呢～可以对我说：你好、谢谢、额度、番茄钟、日程、休息。复杂问题请到 Codex 聊呀。","可以和我打招呼、道谢，或输入额度、番茄钟、日程、休息。复杂问题请回到 Codex 继续。","支持口令：你好、谢谢、额度、番茄钟、日程、休息。复杂问题请使用 Codex。"};break;
  }return lines[index];
 }
}
}
