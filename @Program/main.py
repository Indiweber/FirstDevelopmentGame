import tkinter as tk
from tkinter import ttk, filedialog, messagebox
from excel_processor import ExcelProcessor
from ui_manager import UIManager

class MainApplication:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("Excel to JSON Converter")
        self.root.geometry("800x600")
        
        self.excel_processor = ExcelProcessor()
        self.ui_manager = UIManager(self.root, self.excel_processor)
        
    def run(self):
        self.root.mainloop()

if __name__ == "__main__":
    app = MainApplication()
    app.run() 