using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Pupitre
{
    /// <summary>
    /// Sujet d'une carte mentale : texte, style propre (sinon hérité de la branche ou de la carte), marqueurs,
    /// étiquettes, note, lien, case à faire, légende, limite, résumé, sous-sujets. X/Y ne servent qu'aux sujets flottants.
    /// </summary>
    [DataContract]
    public class MindNode
    {
        [DataMember] public string Id;
        [DataMember] public string Text;
        [DataMember] public int Color;          // 0 = hérité / automatique, 1..9 palette, -1 personnalisée
        [DataMember] public string CustomColor; // #RRGGBB
        [DataMember] public int Shape;          // 0 celle de la carte, 1 arrondi, 2 rectangle, 3 pilule, 4 texte seul, 5 ellipse, 6 souligné
        [DataMember] public int Size;           // 0 normal, 1 grand, 2 petit
        [DataMember] public bool Bold, Italic, Strike;
        [DataMember] public string Marker;      // ancien marqueur unique (repris dans Markers à la lecture)
        [DataMember] public List<string> Markers;
        [DataMember] public List<string> Labels;
        [DataMember] public string Note;
        [DataMember] public string Link;        // adresse web, fichier ou dossier
        [DataMember] public string Callout;     // légende (bulle) attachée au sujet
        [DataMember] public int Todo;           // 0 aucune case, 1 à faire, 2 fait
        [DataMember] public bool Boundary;      // limite autour du sujet et de sa branche
        [DataMember] public string BoundaryLabel;
        [DataMember] public string Summary;     // résumé (accolade) des sous-sujets
        [DataMember] public bool Collapsed;
        [DataMember] public int Side;           // sujets de niveau 1 : 0 automatique, 1 droite / au-dessus, -1 gauche / en dessous
        [DataMember] public double X, Y;        // position d'un sujet flottant
        [DataMember] public List<MindNode> Children;

        public MindNode() { Init(); }
        void Init() { Children = new List<MindNode>(); Markers = new List<string>(); Labels = new List<string>(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Init(); }
        [OnDeserialized]
        void OnDeserialized(StreamingContext c)
        {
            if (Children == null) Children = new List<MindNode>();
            if (Markers == null) Markers = new List<string>();
            if (Labels == null) Labels = new List<string>();
            if (!string.IsNullOrEmpty(Marker))
            {
                string m = Marker;
                if (m == "flag") m = "flag-red"; else if (m == "check") m = "sym-check"; else if (m == "star") m = "star-yellow";
                else if (m == "question") m = "sym-question"; else if (m == "warn") m = "sym-warn"; else if (m == "idea") m = "sym-idea";
                if (!Markers.Contains(m)) Markers.Add(m);
                Marker = null;
            }
        }
    }

    /// <summary>Relation : ligne fléchée libre entre deux sujets, avec un texte.</summary>
    [DataContract]
    public class MindRelation
    {
        [DataMember] public string Id;
        [DataMember] public string From, To;
        [DataMember] public string Label;
    }

    /// <summary>Une carte (feuille) : structure, style global, sujet central, sujets flottants, relations.</summary>
    [DataContract]
    public class MindMap
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public string Created, Updated;
        [DataMember] public string Template;   // modèle de départ (information)
        [DataMember] public int Theme;         // 0 sombre, 1 clair, 2 coloré
        [DataMember] public int Palette;       // palette des branches : 0 Pupitre, 1 océan, 2 forêt, 3 crépuscule, 4 pastel, 5 ardoise
        [DataMember] public int Layout;        // 0 carte bilatérale, 1 logique à droite, 2 organigramme, 3 logique à gauche, 4 arborescence, 5 chronologie, 6 arête de poisson, 7 organigramme vers le haut, 8 chronologie verticale
        [DataMember] public int LineStyle;     // 0 courbes, 1 droites, 2 coudées
        [DataMember] public int LineWidth;     // 0 fine, 1 normale, 2 épaisse
        [DataMember] public int Spacing;       // 0 compact, 1 normal, 2 aéré
        [DataMember] public int Shape;         // forme par défaut des sujets : 1 arrondi, 2 rectangle, 3 pilule, 4 texte seul, 5 ellipse, 6 souligné
        [DataMember] public bool Numbered;     // ancien réglage (repris dans NumberStyle)
        [DataMember] public int NumberStyle;   // 0 aucune, 1 chiffres, 2 lettres, 3 romains
        [DataMember] public bool Tiered;       // numérotation hiérarchique (1.2.1)
        [DataMember] public bool Mono;         // branches d'une seule couleur (sinon arc-en-ciel)
        [DataMember] public bool Tapered;      // lignes effilées : épaisses près du centre, fines aux extrémités
        [DataMember] public MindNode Root;
        [DataMember] public List<MindNode> Floating;
        [DataMember] public List<MindRelation> Relations;
        [DataMember] public double Zoom, PanX, PanY;

        public MindMap() { Init(); }
        void Init() { Zoom = 1; Floating = new List<MindNode>(); Relations = new List<MindRelation>(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Init(); }
        [OnDeserialized]
        void OnDeserialized(StreamingContext c)
        {
            if (Floating == null) Floating = new List<MindNode>();
            if (Relations == null) Relations = new List<MindRelation>();
            if (Numbered && NumberStyle == 0) { NumberStyle = 1; Numbered = false; }
        }
    }

    [DataContract]
    public class MindStore
    {
        [DataMember] public int Version;
        [DataMember] public List<MindMap> Maps;
        [DataMember] public string CurrentId;
        public MindStore() { Maps = new List<MindMap>(); }
        [OnDeserializing] void OnDeserializing(StreamingContext c) { Maps = new List<MindMap>(); }
        [OnDeserialized] void OnDeserialized(StreamingContext c) { if (Maps == null) Maps = new List<MindMap>(); }
    }

    /// <summary>Copies profondes (annuler, dupliquer) par sérialisation.</summary>
    internal static class MindJson
    {
        public static string ToText(MindMap m)
        {
            using (var ms = new MemoryStream()) { new DataContractJsonSerializer(typeof(MindMap)).WriteObject(ms, m); return System.Text.Encoding.UTF8.GetString(ms.ToArray()); }
        }
        public static MindMap FromText(string json)
        {
            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json))) return (MindMap)new DataContractJsonSerializer(typeof(MindMap)).ReadObject(ms);
        }
        public static MindMap Clone(MindMap m) { return FromText(ToText(m)); }
        public static MindNode CloneNode(MindNode n)
        {
            var ser = new DataContractJsonSerializer(typeof(MindNode));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, n);
                ms.Position = 0;
                return (MindNode)ser.ReadObject(ms);
            }
        }
    }
}
