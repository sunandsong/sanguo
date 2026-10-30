using System;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 支线任务。进度全靠剧情标记（StoryFlags）算出来，存档存了标记就等于存了任务进度，这里不另存东西。
    /// Stage：0 = 还没接；1..Steps.Length = 正在做第几步；-1 = 做完了。
    /// </summary>
    public class Quest
    {
        public string Id;
        public string Name;
        public string Giver;              // 谁给的，任务页上写“某某所托”
        public string[] Steps;            // 每一步该做什么，任务页上显示当前这一步
        public string Reward;             // 奖励说明
        public Func<int> Stage = () => 0;
        public Func<string> Progress;     // 进度小字（如“黄巾 3/10”），没有就 null

        public bool Taken { get { return Stage() != 0; } }
        public bool Done { get { return Stage() < 0; } }
    }
}
