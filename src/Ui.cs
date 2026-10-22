using System;
using System.Windows;
using System.Windows.Media;

namespace Pupitre
{
    /// <summary>Parcours de l'arbre visuel, partagé par les panneaux (lignes taguées, éditeurs, conteneurs).</summary>
    internal static class Ui
    {
        static DependencyObject Parent(DependencyObject d)
        {
            return (d is Visual || d is System.Windows.Media.Media3D.Visual3D) ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }

        /// <summary>Premier ancêtre (ou l'élément lui-même) dont le Tag vaut « tag » ; la recherche s'arrête, sans résultat, en atteignant un élément tagué « stopTag ».</summary>
        public static FrameworkElement HitTag(object source, string tag, string stopTag = null)
        {
            var d = source as DependencyObject;
            while (d != null)
            {
                var fe = d as FrameworkElement;
                if (fe != null && tag.Equals(fe.Tag)) return fe;
                if (fe != null && stopTag != null && stopTag.Equals(fe.Tag)) return null;
                d = Parent(d);
            }
            return null;
        }

        public static T FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null)
            {
                var t = d as T;
                if (t != null) return t;
                d = Parent(d);
            }
            return null;
        }

        public static T FindDescendant<T>(DependencyObject root, Func<T, bool> pred) where T : DependencyObject
        {
            if (root == null) return null;
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var ch = VisualTreeHelper.GetChild(root, i);
                var t = ch as T;
                if (t != null && pred(t)) return t;
                var r = FindDescendant<T>(ch, pred);
                if (r != null) return r;
            }
            return null;
        }
    }
}
