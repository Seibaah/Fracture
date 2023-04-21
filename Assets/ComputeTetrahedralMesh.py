import pyvista as pv
import tetgen
import numpy as np

cube = pv.Cube(center=(0.0, 0.0, 0.0), x_length=6, y_length=6, z_length=1).triangulate()

tet = tetgen.TetGen(cube)
tet.tetrahedralize(order=1, mindihedral=25, minratio=1.5)
grid = tet.grid

# extract the vertices positions as a numpy array
vertices = np.array(grid.points)

# extract the tetrahedra as a numpy array of point indices
tetrahedra = np.array(grid.cells.reshape(-1, 5)[:, 1:])

np.savetxt('verts.csv', vertices, fmt='%0.2f', delimiter=',')
np.savetxt('tets.csv', tetrahedra, fmt='%d', delimiter=',')

# print the number of vertices and tetrahedra
print('Number of vertices:', vertices.shape[0])
# print(vertices)
print('Number of tetrahedra:', tetrahedra.shape[0])
# print(tetrahedra)

pv.set_plot_theme('paraview')
grid.plot(show_edges=True)