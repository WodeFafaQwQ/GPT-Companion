using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DragonCompanion {
static class PlanTopics {
 static readonly Regex ActivityWords=new Regex(@"短期冲刺|中长期|每日任务|阶段任务|题型|手写|代码|基础|进阶|入门|学习|掌握|理解|讲解|阅读|回顾|复习|复盘|回顾|复现|练习|训练|刷题|做题|解题|巩固|实战|测试|准备|验收|总结|整理|错题|竞赛|模板|专项|混合|综合|例题|尝试|算法|知识点|相关|本日|今日|昨天|昨日|上一节|上一天|第[一二三四五六七八九十]+天|(?:\b(?:day|lesson|study|review|practice|exercise|introduction|basics|training|quiz|recap|problems?|project)\b)|[与及和]",RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
 static readonly Regex Words=new Regex(@"[\u3400-\u9fff]{2,}|[a-z][a-z0-9+#.-]+",RegexOptions.Compiled|RegexOptions.CultureInvariant);

 static string Normalize(string text){return (text??"").Normalize(NormalizationForm.FormKC).Trim().ToLowerInvariant();}
 static string[] Tokens(string text){return Words.Matches(ActivityWords.Replace(Normalize(text)," ")).Cast<Match>().Select(match=>match.Value.TrimEnd('.','-')).Distinct().ToArray();}
 static bool Han(string word){return word.Length>0&&word[0]>='\u3400'&&word[0]<='\u9fff';}
 static bool MatchWords(string[] a,string[] b,bool requireSpecificTerm=false){
  return a.Any(first=>b.Any(second=>{
   if(first==second)return !requireSpecificTerm||!Han(first)||first.Length>=3;
   return Han(first)&&Han(second)&&(first.Length>=3&&second.Contains(first)||second.Length>=3&&first.Contains(second));
  }));
 }
 internal static string[] Clean(IEnumerable<string> topics){return (topics??Enumerable.Empty<string>()).Where(topic=>!string.IsNullOrWhiteSpace(topic)).Select(topic=>topic.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();}
 internal static Func<PlanDay,bool> RelatedTo(PlanDay source){
  var topics=Clean(source.Topics).Select(Normalize).ToArray();
  var titleWords=Tokens(topics.Length>0?string.Join(" ",topics):source.Title);
  var noteWords=Tokens(source.Notes);
  return candidate=>{
   var otherTopics=Clean(candidate.Topics).Select(Normalize).ToArray();
   if(topics.Length>0&&otherTopics.Length>0)return topics.Intersect(otherTopics,StringComparer.Ordinal).Any();
   var otherTitle=Tokens(otherTopics.Length>0?string.Join(" ",otherTopics):candidate.Title);
   return MatchWords(titleWords,otherTitle)||MatchWords(titleWords,Tokens(candidate.Notes),true)||MatchWords(otherTitle,noteWords,true);
  };
 }
}
}
