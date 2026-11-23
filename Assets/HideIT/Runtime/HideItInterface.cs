using UnityEngine;

namespace HideIT
{
    public sealed partial class CarryLabBootstrap
    {
        static readonly Color BasePanel = new Color(.065f,.085f,.077f,.97f);
        static readonly Color RaisedPanel = new Color(.105f,.13f,.113f,1);
        static readonly Color Border = new Color(.27f,.34f,.28f,1);
        static readonly Color TextColor = new Color(.94f,.94f,.86f);
        static readonly Color Muted = new Color(.71f,.76f,.69f);
        static readonly Color Mint = new Color(.78f,.89f,.64f);
        static readonly Color Alarm = new Color(1f,.55f,.43f);
        static readonly Color Cool = new Color(.66f,.85f,.93f);
        GUIStyle titleType, sectionType, rowType, smallType, centeredType, centerSmall;

        void OnGUI()
        {
            if (Player == null) return;
            if (titleType == null) CreateTypes();
            var previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)*.5f,(Screen.height-720*scale)*.5f),
                Quaternion.identity,Vector3.one*scale);
            if (Caught || Delivered) DrawResult();
            else if (showingGuide) DrawGuide();
            else if (!Started) DrawTitle();
            else if (choosingOutfit) DrawPauseRoster();
            else if (Paused) DrawPause();
            else DrawHud();
            GUI.matrix = previous;
        }

        void CreateTypes()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rowType = new GUIStyle(GUI.skin.label) { font = font,fontSize = 18,alignment = TextAnchor.MiddleLeft,wordWrap = false };
            smallType = new GUIStyle(rowType) { fontSize = 14 };
            titleType = new GUIStyle(rowType) { fontSize = 83,fontStyle = FontStyle.Bold };
            sectionType = new GUIStyle(rowType) { fontSize = 34,fontStyle = FontStyle.Bold };
            centeredType = new GUIStyle(rowType) { alignment = TextAnchor.MiddleCenter,fontStyle = FontStyle.Bold };
            centerSmall = new GUIStyle(smallType) { alignment = TextAnchor.MiddleCenter };
        }

        void DrawTitle()
        {
            Veil(.43f);
            Panel(new Rect(73,88,509,544));
            Fill(new Rect(73,88,5,544),Mint);
            Write(new Rect(112,120,425,30),"ZANETTIZONEAI    /    PLAYTEST " + Application.version,smallType,Muted);
            Write(new Rect(107,159,420,106),"Hide iT",titleType,TextColor);
            Write(new Rect(112,285,425,44),MissionName,sectionType,Mint);
            Write(new Rect(112,353,423,36),InAsylum ? "Stash the body in the service dumpster." : "Bring the body to the green zone.",rowType,TextColor);
            if (Button(new Rect(112,434,426,56),InAsylum ? "ENTER ASYLUM   [ENTER]" : "ENTER CARRY LAB   [ENTER]",true)) Begin();
            if (Button(new Rect(112,503,426,48),"CONTROLS")) ShowGuide(true);
            DrawRoster(new Rect(626,88,581,544));
        }

        void DrawPause()
        {
            Veil(.69f);
            Panel(new Rect(409,122,462,479));
            Write(new Rect(446,153,387,30),MissionName,smallType,Muted);
            Write(new Rect(445,186,390,58),"Paused",sectionType,TextColor);
            if (Button(new Rect(445,264,390,52),"RESUME   [ESC]",true)) Pause(false);
            if (Button(new Rect(445,327,390,48),"CHANGE CHARACTER")) ShowPauseOutfits(true);
            if (Button(new Rect(445,386,390,48),"CONTROLS")) ShowGuide(true);
            if (Button(new Rect(445,445,390,48),"RESTART MISSION")) Retry();
        }

        void DrawPauseRoster()
        {
            Veil(.69f);
            DrawRoster(new Rect(350,64,580,553));
            if (Button(new Rect(350,631,580,46),"BACK TO PAUSE   [ESC]")) ShowPauseOutfits(false);
        }

        void DrawRoster(Rect area)
        {
            Panel(area);
            Write(new Rect(area.x+31,area.y+24,area.width-62,38),"CHANGE CHARACTER",smallType,Muted);
            var inset = new Rect(area.x+30,area.y+72,area.width-60,area.height-219);
            Fill(inset,new Color(.043f,.056f,.052f,1));
            if (characterPreview.Texture != null) GUI.DrawTexture(inset,characterPreview.Texture,ScaleMode.ScaleToFit);
            var outfit = CharacterArtCatalog.Players[SelectedOutfit];
            Write(new Rect(area.x+31,inset.yMax+8,area.width-62,47),outfit.Name,sectionType,TextColor);
            Write(new Rect(area.x+31,inset.yMax+55,area.width-62,23),outfit.Description,smallType,Muted);
            float gap = 7, width = (area.width-60-gap*4)/5f;
            for (int i = 0; i < CharacterArtCatalog.Players.Length; i++)
                if (Button(new Rect(area.x+30+i*(width+gap),area.yMax-57,width,37),
                        CharacterArtCatalog.Players[i].Name,i == SelectedOutfit)) SelectOutfit(i);
        }

        void DrawGuide()
        {
            Veil(.72f);
            Panel(new Rect(205,72,870,579));
            Write(new Rect(244,99,785,55),"Controls",sectionType,TextColor);
            Fill(new Rect(244,160,793,1),Border);
            Write(new Rect(244,177,364,30),"MOVEMENT",smallType,Mint);
            Write(new Rect(666,177,364,30),"INTERACT",smallType,Mint);
            GuideRow(244,220,"W A S D","Move");
            GuideRow(244,269,"Mouse","Look and aim");
            GuideRow(244,318,"Shift","Run");
            GuideRow(244,367,"Ctrl / C","Crouch");
            GuideRow(244,416,"V","Switch camera");
            GuideRow(666,220,"E",InAsylum ? "Grab, stash, hide or pick up" : "Grab or release");
            GuideRow(666,269,"Q","Drop the body");
            GuideRow(666,318,"Hold LMB","Raise your grip");
            if (InAsylum) GuideRow(666,367,"G","Throw a rock where you aim");
            GuideRow(666,416,"R / Esc","Restart / Pause");
            if (InAsylum) Write(new Rect(244,484,793,40),"Break line of sight before hiding. Rock impacts draw nearby guards.",rowType,Muted);
            if (Button(new Rect(244,554,793,50),"BACK   [ESC]",true)) ShowGuide(false);
        }

        void GuideRow(float x,float y,string key,string action)
        {
            Fill(new Rect(x,y,111,37),RaisedPanel);
            Write(new Rect(x,y,111,37),key,centerSmall,TextColor);
            Write(new Rect(x+128,y,254,37),action,rowType,TextColor);
        }

        void DrawHud()
        {
            var actions = Interactions;
            DrawTag(new Rect(18,16,206,32),InAsylum ? "STASH THE BODY" : "BRING BODY TO EXIT",Mint);
            if (actions != null) DrawTag(new Rect(1145,16,117,32),$"{actions.Rocks:00}  ROCKS",TextColor);
            bool chase = false, search = false, body = false;
            float distracted = 0;
            foreach (var guard in Guards)
            {
                chase |= guard.State == AsylumGuard.Activity.Chase;
                search |= guard.State == AsylumGuard.Activity.Search;
                body |= guard.State == AsylumGuard.Activity.InspectBody;
                distracted = Mathf.Max(distracted,guard.DistractionSeconds);
            }
            if (actions != null && actions.Hidden) DrawTag(new Rect(550,16,180,33),search ? "HIDDEN / SEARCHING" : "HIDDEN",Mint);
            else if (chase) DrawTag(new Rect(550,16,180,33),"CHASED",Alarm);
            else if (search) DrawTag(new Rect(550,16,180,33),"GUARDS SEARCHING",Alarm);
            else if (distracted > 0) DrawTag(new Rect(534,16,212,33),$"DISTRACTED   {Mathf.CeilToInt(distracted):00}s",Cool);
            else if (body) DrawTag(new Rect(550,16,180,33),"BODY DISCOVERED",Cool);
            if (noticeTime > 0) SmallPrompt(Notice,581,Alarm);

            Fill(new Rect(638,358,4,4),new Color(1,1,1,.83f));
            string prompt = actions != null ? actions.Prompt : null;
            if (prompt == null)
            {
                if (Player.Holding) prompt = Player.Lifted ? "RAISING  /  " + Player.HeldLabel : "CARRYING  /  " + Player.HeldLabel;
                else if (Player.TryFindGrip(out var grip)) prompt = "[ E ] Grab " + grip.Label;
            }
            if (!string.IsNullOrEmpty(prompt)) SmallPrompt(prompt,421,TextColor);
        }

        void DrawResult()
        {
            Veil(.63f);
            Panel(new Rect(368,218,544,281));
            Color highlight = Caught ? Alarm : Mint;
            Fill(new Rect(368,218,544,3),highlight);
            Write(new Rect(406,248,453,65),Caught ? "Caught" : InAsylum ? "Body stashed" : "Level complete",sectionType,highlight);
            string description = Caught ? CaughtReason : InAsylum ? "Level complete" : "The body reached the drop zone.";
            Write(new Rect(406,323,453,45),description,rowType,TextColor);
            if (Button(new Rect(407,398,466,54),"RESTART   [R]",true)) Retry();
        }

        void DrawTag(Rect rect,string value,Color color)
        {
            Fill(rect,new Color(.028f,.037f,.031f,.72f));
            Fill(new Rect(rect.x,rect.y,2,rect.height),color);
            Write(new Rect(rect.x+8,rect.y,rect.width-16,rect.height),value,centerSmall,color);
        }

        void SmallPrompt(string value,float y,Color color)
        {
            float width = Mathf.Clamp(centerSmall.CalcSize(new GUIContent(value)).x+30,130,520);
            var rect = new Rect((1280-width)*.5f,y,width,31);
            Fill(rect,new Color(.028f,.037f,.031f,.78f));
            Write(rect,value,centerSmall,color);
        }

        bool Button(Rect rect,string text,bool primary=false)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            var bg = primary ? hover ? new Color(.88f,.95f,.75f) : Mint : hover ? Border : RaisedPanel;
            Fill(rect,bg);
            Write(rect,text,centeredType,primary ? new Color(.055f,.08f,.056f) : TextColor);
            return GUI.Button(rect,GUIContent.none,GUIStyle.none);
        }

        static void Veil(float opacity) => Fill(new Rect(0,0,1280,720),new Color(.024f,.034f,.028f,opacity));

        static void Panel(Rect rect)
        {
            Fill(rect,BasePanel);
            Fill(new Rect(rect.x,rect.y,rect.width,1),Border);
        }

        static void Write(Rect rect,string value,GUIStyle type,Color color)
        {
            type.normal.textColor = color;
            GUI.Label(rect,value,type);
        }

        static void Fill(Rect rect,Color color)
        {
            var saved = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color = saved;
        }
    }
}
