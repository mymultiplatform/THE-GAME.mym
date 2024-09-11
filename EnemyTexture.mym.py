import tkinter as tk
import random

# Create the main application window
root = tk.Tk()
root.title("Animated Liquid Lava Lamp Effect")
canvas_size = 900

# Create a canvas in the Tkinter window
canvas = tk.Canvas(root, width=canvas_size, height=canvas_size)
canvas.pack()

# Blob class to handle each blob's properties and animation
class Blob:
    def __init__(self, x, y, radius, color, canvas):
        self.x = x
        self.y = y
        self.radius = radius
        self.color = color
        self.canvas = canvas
        self.blob_id = None
        self.dx = random.uniform(-2, 2)  # Random x direction
        self.dy = random.uniform(-2, 2)  # Random y direction
        self.growth = random.uniform(-1, 1)  # Random growth/shrink speed

    def draw(self):
        if self.blob_id is not None:
            self.canvas.delete(self.blob_id)
        self.blob_id = self.canvas.create_oval(
            self.x - self.radius, self.y - self.radius,
            self.x + self.radius, self.y + self.radius,
            fill=self.color, outline=self.color)

    def update(self):
        # Update the position of the blob
        self.x += self.dx
        self.y += self.dy

        # Make the blob grow and shrink
        self.radius += self.growth

        # Reverse direction when the blob gets too big or too small
        if self.radius < 30 or self.radius > 150:
            self.growth = -self.growth

        # Bounce off the walls
        if self.x - self.radius <= 0 or self.x + self.radius >= canvas_size:
            self.dx = -self.dx
        if self.y - self.radius <= 0 or self.y + self.radius >= canvas_size:
            self.dy = -self.dy

        # Redraw the blob in the new position
        self.draw()

# Function to animate the blobs
def animate():
    for blob in blobs:
        blob.update()
    root.after(30, animate)  # Call the function again after 30 ms for continuous animation

# Parameters for blobs
num_blobs = 20  # Number of blobs
max_radius = 100  # Maximum radius of blobs

# Create blobs with random properties
blobs = []
for _ in range(num_blobs):
    x = random.randint(0, canvas_size)
    y = random.randint(0, canvas_size)
    radius = random.randint(50, max_radius)
    rand_value = random.randint(0, 100)
    if rand_value < 33:
        color = "#0057b7"  # Blue-ish
    elif rand_value < 66:
        color = "#a6c050"  # Green-ish
    else:
        color = "#d4b234"  # Yellow-ish
    blob = Blob(x, y, radius, color, canvas)
    blobs.append(blob)
    blob.draw()

# Start the animation loop
animate()

# Run the Tkinter event loop
root.mainloop()
