import clr, random

clr.AddReference('RevitAPI')
clr.AddReference('RevitServices')

from Autodesk.Revit.DB import *
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

doc = DocumentManager.Instance.CurrentDBDocument

# ===================== MATERIAL CREATION =====================
def get_or_create_material(name, color):
    mats = FilteredElementCollector(doc).OfClass(Material)
    for m in mats:
        if m.Name == name:
            return m

    mat_id = Material.Create(doc, name)
    mat = doc.GetElement(mat_id)
    mat.Color = Color(color[0], color[1], color[2])
    mat.Shininess = 20
    mat.Smoothness = 0.4
    return mat


# ===================== DEFAULT MATERIAL PALETTE =====================
building_mats = [
    get_or_create_material("B1_Concrete", (200,200,200)),
    get_or_create_material("B2_Brick",    (170,120,100)),
    get_or_create_material("B3_Glass",    (180,230,255)),
    get_or_create_material("B4_Steel",    (150,160,170))
]

road_mat = get_or_create_material("Road_Asphalt", (70,70,70))


# ===================== APPLY MATERIAL TO ELEMENTS =====================
def apply_material_to_element(elem, mat):
    param = elem.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM)
    if param and not param.IsReadOnly:
        param.Set(mat.Id)


# ===================== CITY LOOP (you already generated geometry) ====
TransactionManager.Instance.EnsureInTransaction(doc)

all_walls  = list(FilteredElementCollector(doc).OfClass(Wall))
all_floors = list(FilteredElementCollector(doc).OfClass(Floor))

# Roads (large slabs)
for f in all_floors:
    if f.LookupParameter("Area").AsDouble() > 400:   # large = street
        apply_material_to_element(f, road_mat)

# Buildings
for w in all_walls:
    mat = random.choice(building_mats)
    apply_material_to_element(w, mat)

TransactionManager.Instance.TransactionTaskDone()

OUT = "Materials assigned to buildings + streets 🎨"
