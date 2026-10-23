using System.Collections.Generic;
using System.Linq;

namespace Pupitre
{
    /// <summary>
    /// Cadres fusionnés : une colonne de cadres collés les uns sous les autres (même abscisse, même largeur), déplacée d'un
    /// bloc ; chaque cadre garde son titre et se replie dessus (double-clic). La chaîne est portée par CadreInfo.Under :
    /// identifiant du cadre juste au-dessus (null = cadre indépendant ou sommet d'un groupe).
    /// </summary>
    public static class Fusion
    {
        static bool syncing;

        public static CadreWindow ById(Controller c, string id) { return string.IsNullOrEmpty(id) ? null : c.Cadres.FirstOrDefault(w => w.Info.Id == id); }
        public static CadreWindow Above(Controller c, CadreWindow w) { return ById(c, w.Info.Under); }
        public static CadreWindow Below(Controller c, CadreWindow w) { return c.Cadres.FirstOrDefault(x => x.Info.Under == w.Info.Id); }
        public static bool IsBelow(Controller c, CadreWindow w) { return Above(c, w) != null; }
        public static bool IsGrouped(Controller c, CadreWindow w) { return Above(c, w) != null || Below(c, w) != null; }

        public static CadreWindow Top(Controller c, CadreWindow w)
        {
            var seen = new HashSet<CadreWindow> { w };
            var cur = w;
            while (true) { var up = Above(c, cur); if (up == null || !seen.Add(up)) return cur; cur = up; }
        }

        /// <summary>Membres du groupe, du haut vers le bas (le cadre seul s'il n'est pas fusionné).</summary>
        public static List<CadreWindow> Chain(Controller c, CadreWindow w)
        {
            var list = new List<CadreWindow>();
            var cur = Top(c, w);
            while (cur != null && !list.Contains(cur)) { list.Add(cur); cur = Below(c, cur); }
            return list;
        }

        /// <summary>Sommets de colonne auxquels un cadre peut se joindre (tous sauf lui-même ; un cadre placé sous lui
        /// n'est jamais un sommet, donc aucun cycle possible). Le sommet de sa propre colonne y figure : choix courant.</summary>
        public static List<CadreWindow> Targets(Controller c, CadreWindow w)
        {
            return c.Cadres.Where(t => t != w && Above(c, t) == null).ToList();
        }

        /// <summary>Après un déplacement, un redimensionnement ou un repli d'un membre : tout le groupe suit.</summary>
        public static void Sync(Controller c, DesktopWindow source)
        {
            var src = source as CadreWindow;
            if (src == null || syncing || !src.IsVisible || !IsGrouped(c, src)) return;
            syncing = true;
            try
            {
                var chain = Chain(c, src).Where(m => m.IsVisible).ToList();
                int at = chain.IndexOf(src);
                if (at < 0) return;
                double x = src.Left, w = src.Width, y = src.Top;
                for (int i = at - 1; i >= 0; i--) y -= HeightOf(chain[i]);      // ordonnée du sommet d'après le membre déplacé
                for (int i = 0; i < chain.Count; i++)
                {
                    chain[i].PlaceInGroup(x, y, w, i == 0, i == chain.Count - 1);
                    y += HeightOf(chain[i]);
                }
            }
            finally { syncing = false; }
        }

        static double HeightOf(DesktopWindow m) { return m.ActualHeight > 0 ? m.ActualHeight : m.Height; }

        /// <summary>Réaligne tous les groupes (démarrage, disposition appliquée, changement d'écrans).</summary>
        public static void SyncAll(Controller c)
        {
            foreach (var w in c.Cadres.ToList())
            {
                if (!w.IsVisible) continue;
                if (IsGrouped(c, w)) Sync(c, w); else w.SetGroupCorners(true, true);
            }
        }

        /// <summary>Retire un cadre de son groupe ; ceux qui étaient en dessous remontent d'un cran dans la chaîne.</summary>
        public static void Detach(Controller c, CadreWindow w)
        {
            if (!IsGrouped(c, w)) return;
            var below = Below(c, w);
            if (below != null) below.Info.Under = w.Info.Under;
            w.Info.Under = null;
            w.SetGroupCorners(true, true);
            if (below != null) { if (IsGrouped(c, below)) Sync(c, below); else below.SetGroupCorners(true, true); }
            c.SaveLayoutSoon();
        }

        /// <summary>Place un cadre en bas de la colonne de « target » (null : le libère).</summary>
        public static void Attach(Controller c, CadreWindow w, CadreWindow target)
        {
            Detach(c, w);
            if (target == null || target == w) return;
            var chain = Chain(c, target);
            if (chain.Contains(w)) return;
            w.Info.Under = chain[chain.Count - 1].Info.Id;
            if (!w.IsVisible && w.Info.Visible) w.Show();
            Sync(c, chain[0]);
            c.SaveLayoutSoon();
        }
    }
}
