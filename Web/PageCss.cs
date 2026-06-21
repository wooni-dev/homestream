namespace HomeStream.Web;

internal static class PageCss
{
    internal const string Value = """
        * { box-sizing: border-box; margin: 0; padding: 0; -webkit-tap-highlight-color: transparent; }
        body { background:#0e0e12; color:#ececf1; font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif; padding-bottom:40px; }
        header { position:sticky; top:0; background:rgba(14,14,18,.92); backdrop-filter:blur(8px);
          padding:16px 18px 12px; border-bottom:1px solid #23232c; z-index:10; }
        header h1 { font-size:17px; font-weight:700; color:#fff; }
        header .sub { font-size:12px; color:#7d7d8c; margin-top:3px; }
        .crumb { font-size:13px; color:#8a8aff; margin-top:10px; line-height:1.6; word-break:break-all; }
        .crumb a { color:#8a8aff; text-decoration:none; }
        .list { padding:8px 12px; }
        .item { display:flex; align-items:center; gap:13px; padding:14px 14px; margin:7px 0;
          background:#191922; border:1px solid #23232c; border-radius:14px; text-decoration:none;
          color:#ececf1; transition:background .12s; }
        .item:active { background:#22222e; }
        .ic { width:42px; height:42px; flex:none; border-radius:11px; display:flex; align-items:center;
          justify-content:center; font-size:20px; }
        .ic.folder { background:#2a2540; }
        .ic.video { background:#3a1f2a; }
        .meta { flex:1; min-width:0; }
        .name { font-size:15px; font-weight:500; line-height:1.35; word-break:break-all; }
        .sz { font-size:12px; color:#7d7d8c; margin-top:3px; }
        .chev { color:#55556a; font-size:18px; flex:none; }
        .empty { text-align:center; color:#666; padding:50px 0; font-size:14px; }
        .pbody { background:#000; display:flex; flex-direction:column; min-height:100vh; }
        .ptop { padding:14px 16px; display:flex; align-items:center; gap:12px; background:#0e0e12; }
        .back { color:#8a8aff; text-decoration:none; font-size:15px; font-weight:600; flex:none; }
        .ptitle { font-size:14px; color:#ddd; line-height:1.35; word-break:break-all; }
        .pwrap { flex:1; display:flex; align-items:center; justify-content:center; background:#000; }
        video { width:100%; max-height:100%; background:#000; }
        """;
}
