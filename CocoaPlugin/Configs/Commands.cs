using System.Collections.Generic;

namespace CocoaPlugin.Configs;

public class Commands
{
    public string CommandNotFound { get; set; } = "해당 명령어를 찾을 수 없습니다.";

    public string ExecuteSuccessColor { get; set; } = "white";
    public string ExecuteFailColor { get; set; } = "red";

    public string ExecuteErrorText { get; set; } = "명령어 실행 중 오류가 발생했습니다. 빠르게 기술 지원팀에 문의하십시오.\n";
}