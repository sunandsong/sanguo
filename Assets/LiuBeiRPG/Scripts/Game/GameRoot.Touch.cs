using UnityEngine;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 手机虚拟按键（只在大地图上、没有对话框时出现）：左下摇杆走路，右下 确认 / 取消。
    /// 用 Input.touches 读多指（IMGUI 只认第一根手指，按着摇杆再点按钮会丢），结果塞进 held / OnKey，跟键盘走同一条路。
    /// 对话、选项、战斗本来就能直接点，不用这套。
    /// </summary>
    public partial class GameRoot
    {
        const float JoyR = 70f, KnobR = 30f, DeadZone = 18f;
        const float BtnA = 46f, BtnB = 36f;

        int joyFinger = -1;
        Vector2 joyBase, joyKnob;
        InputKey? joyKey;
        float pressA = -10f, pressB = -10f;   // 按下的时刻，画按下的缩放

        // UnityEngine.Device 版会跟着 Device Simulator 变；UnityEngine.Application 在编辑器里永远是 false
        static bool TouchUi { get { return UnityEngine.Device.Application.isMobilePlatform; } }

        bool TouchUiShown { get { return TouchUi && mode == Mode.Map && !UiBusy; } }

        // 静止时摇杆底座、两个按钮的位置（虚拟坐标，贴安全区左下 / 右下）
        static Vector2 JoyHome { get { var s = Gfx.SafeArea; return new Vector2(s.x + 40 + JoyR, s.yMax - 40 - JoyR); } }
        static Vector2 BtnAPos { get { var s = Gfx.SafeArea; return new Vector2(s.xMax - 36 - BtnA, s.yMax - 70 - BtnA); } }
        static Vector2 BtnBPos { get { var s = Gfx.SafeArea; return new Vector2(BtnAPos.x - BtnA - 30 - BtnB, s.yMax - 30 - BtnB); } }

        static Vector2 ToVirtual(Vector2 p) { return new Vector2(p.x * Gfx.VW / Screen.width, (Screen.height - p.y) * Gfx.VH / Screen.height); }

        void UpdateTouch()
        {
            if (!TouchUiShown) { ReleaseJoy(); return; }
            foreach (var t in Touches())
            {
                var p = ToVirtual(t.position);
                if (t.phase == TouchPhase.Began)
                {
                    if ((p - BtnAPos).magnitude < BtnA + 12) { pressA = Time.time; OnKey(InputKey.Confirm); }
                    else if ((p - BtnBPos).magnitude < BtnB + 12) { pressB = Time.time; OnKey(InputKey.Cancel); }
                    else if (joyFinger < 0 && p.x < Gfx.VW * 0.5f) { joyFinger = t.fingerId; joyBase = p; joyKnob = p; }   // 左半屏哪里按下，摇杆就挪到哪里
                }
                else if (t.fingerId == joyFinger)
                {
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { ReleaseJoy(); continue; }
                    var d = Vector2.ClampMagnitude(p - joyBase, JoyR);
                    joyKnob = joyBase + d;
                    // 只走四方向：取偏得多的那根轴
                    InputKey? k = d.magnitude < DeadZone ? (InputKey?)null
                        : Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x > 0 ? InputKey.Right : InputKey.Left)
                        : (d.y > 0 ? InputKey.Down : InputKey.Up);
                    if (k == joyKey) continue;
                    if (joyKey != null) held.Remove(joyKey.Value);
                    joyKey = k;
                    if (k != null) { held.Add(k.Value); OnKey(k.Value); }
                    if (!TouchUiShown) { ReleaseJoy(); return; }   // 这一步踩出了对话或战斗
                }
            }
        }

        // 没有真触摸时拿鼠标冒充一根手指：Device Simulator 有时只给鼠标事件，编辑器里也能点着测
        static System.Collections.Generic.IEnumerable<Touch> Touches()
        {
            if (Input.touchCount > 0) { foreach (var t in Input.touches) yield return t; yield break; }
            TouchPhase ph;
            if (Input.GetMouseButtonDown(0)) ph = TouchPhase.Began;
            else if (Input.GetMouseButtonUp(0)) ph = TouchPhase.Ended;
            else if (Input.GetMouseButton(0)) ph = TouchPhase.Moved;
            else yield break;
            yield return new Touch { fingerId = 99, phase = ph, position = Input.mousePosition };
        }

        void ReleaseJoy()
        {
            if (joyKey != null) held.Remove(joyKey.Value);
            joyKey = null;
            joyFinger = -1;
        }

        void DrawTouchUi()
        {
            if (!TouchUiShown || Event.current.type != EventType.Repaint) return;
            bool on = joyFinger >= 0;
            var c = on ? joyBase : JoyHome;
            Disc(c, JoyR, new Color(0, 0, 0, 0.28f));
            Ring(c, JoyR, new Color(1, 1, 1, on ? 0.5f : 0.3f));
            Disc(on ? joyKnob : c, KnobR, new Color(1, 1, 1, on ? 0.7f : 0.45f));
            TouchButton(BtnAPos, BtnA, "确认", pressA);
            TouchButton(BtnBPos, BtnB, "取消", pressB);
        }

        void TouchButton(Vector2 c, float r, string label, float pressed)
        {
            float k = Time.time - pressed < 0.12f ? 0.9f : 1f;
            Disc(c, r * k, new Color(0, 0, 0, 0.35f));
            Ring(c, r * k, new Color(Accent.r, Accent.g, Accent.b, 0.7f));
            Gfx.Text(new Rect(c.x - r, c.y - r, r * 2, r * 2), label, r > 40 ? 20 : 17, new Color(1, 1, 1, 0.9f), TextAnchor.MiddleCenter);
        }

        static void Disc(Vector2 c, float r, Color col) { Tinted(new Rect(c.x - r, c.y - r, r * 2, r * 2), DiscTex(), col); }
        // RingTex 的圈画在 0.82 半径处，放大一点让它贴着圆盘边
        static void Ring(Vector2 c, float r, Color col) { r /= 0.82f; Tinted(new Rect(c.x - r, c.y - r, r * 2, r * 2), RingTex(), col); }

        static void Tinted(Rect r, Texture2D tex, Color col)
        {
            var old = GUI.color;
            GUI.color = col;
            Gfx.Image(r, tex);
            GUI.color = old;
        }

        static Texture2D discTex;

        static Texture2D DiscTex()
        {
            if (discTex != null) return discTex;
            const int n = 64;
            discTex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                    discTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1f - d) * 16f)));
                }
            discTex.Apply();
            return discTex;
        }
    }
}
