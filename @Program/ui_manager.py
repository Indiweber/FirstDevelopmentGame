import tkinter as tk
from tkinter import ttk, filedialog
import os
import json

class UIManager:
    def __init__(self, root, excel_processor):
        self.root = root
        self.excel_processor = excel_processor
        self.excel_files = {}  # {파일명: 체크박스_변수}
        self.confirmed_files = {}  # {파일명: 체크박스_변수}
        self.config_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), "config.json")
        
        self.setup_ui()
        self.load_paths()  # 저장된 경로 불러오기
    
    def setup_ui(self):
        """UI 구성"""
        # 경로 설정 프레임 (전체 높이의 15%)
        path_frame = ttk.LabelFrame(self.root, text="경로 설정", padding=10)
        path_frame.pack(fill="x", padx=5, pady=5)
        
        # 참조 경로
        source_frame = ttk.Frame(path_frame)
        source_frame.pack(fill="x")
        ttk.Label(source_frame, text="참조 경로:").pack(side="left")
        self.source_path_var = tk.StringVar()
        source_path_entry = ttk.Entry(source_frame, textvariable=self.source_path_var, width=50)
        source_path_entry.pack(side="left", padx=5, expand=True, fill="x")
        ttk.Button(source_frame, text="경로 설정", command=self.set_source_path).pack(side="right")
        
        # 저장 경로
        target_frame = ttk.Frame(path_frame)
        target_frame.pack(fill="x", pady=5)
        ttk.Label(target_frame, text="저장 경로:").pack(side="left")
        self.target_path_var = tk.StringVar()
        target_path_entry = ttk.Entry(target_frame, textvariable=self.target_path_var, width=50)
        target_path_entry.pack(side="left", padx=5, expand=True, fill="x")
        ttk.Button(target_frame, text="경로 설정", command=self.set_target_path).pack(side="right")
        
        # 문서 리스트 프레임 (전체 높이의 40%)
        docs_frame = ttk.LabelFrame(self.root, text="문서 리스트", padding=10)
        docs_frame.pack(fill="both", padx=5, pady=5)
        
        # 선택/확정 리스트 프레임
        lists_frame = ttk.Frame(docs_frame)
        lists_frame.pack(fill="both", expand=True)
        
        # 리스트 프레임들의 weight 설정
        lists_frame.grid_columnconfigure(0, weight=45)  # 선택 리스트
        lists_frame.grid_columnconfigure(1, weight=10)  # 버튼
        lists_frame.grid_columnconfigure(2, weight=45)  # 확정 리스트
        
        # 선택 리스트 (체크박스 포함) - 45% 너비
        select_frame = ttk.LabelFrame(lists_frame, text="선택", padding=5)
        select_frame.grid(row=0, column=0, sticky="nsew", padx=5)
        
        # 선택 리스트 스크롤바
        select_scrollbar = ttk.Scrollbar(select_frame)
        select_scrollbar.pack(side="right", fill="y")
        
        # 선택 리스트 캔버스
        self.select_canvas = tk.Canvas(select_frame)
        self.select_canvas.pack(side="left", fill="both", expand=True)
        
        # 선택 리스트 스크롤바 연결
        select_scrollbar.config(command=self.select_canvas.yview)
        self.select_canvas.config(yscrollcommand=select_scrollbar.set)
        
        # 선택 리스트 체크박스 프레임
        self.select_checkbox_frame = ttk.Frame(self.select_canvas)
        self.select_canvas.create_window((0, 0), window=self.select_checkbox_frame, anchor="nw")
        self.select_checkbox_frame.bind("<Configure>", lambda e: self.select_canvas.configure(scrollregion=self.select_canvas.bbox("all")))
        
        # 버튼 프레임 - 10% 너비
        button_frame = ttk.Frame(lists_frame)
        button_frame.grid(row=0, column=1, sticky="ns", padx=10)
        
        # 버튼을 담을 컨테이너 (가운데 정렬을 위해)
        button_container = ttk.Frame(button_frame)
        button_container.pack(expand=True)  # expand=True로 설정하여 세로 중앙 정렬
        
        ttk.Button(button_container, text="➡", command=self.add_to_confirmed).pack(pady=5)
        ttk.Button(button_container, text="⬅", command=self.remove_from_confirmed).pack(pady=5)
        
        # 확정 리스트 - 45% 너비
        confirm_frame = ttk.LabelFrame(lists_frame, text="확정", padding=5)
        confirm_frame.grid(row=0, column=2, sticky="nsew", padx=5)
        
        # 확정 리스트 스크롤바
        confirm_scrollbar = ttk.Scrollbar(confirm_frame)
        confirm_scrollbar.pack(side="right", fill="y")
        
        # 확정 리스트 캔버스
        self.confirm_canvas = tk.Canvas(confirm_frame)
        self.confirm_canvas.pack(side="left", fill="both", expand=True)
        
        # 확정 리스트 스크롤바 연결
        confirm_scrollbar.config(command=self.confirm_canvas.yview)
        self.confirm_canvas.config(yscrollcommand=confirm_scrollbar.set)
        
        # 확정 리스트 체크박스 프레임
        self.confirm_checkbox_frame = ttk.Frame(self.confirm_canvas)
        self.confirm_canvas.create_window((0, 0), window=self.confirm_checkbox_frame, anchor="nw")
        self.confirm_checkbox_frame.bind("<Configure>", lambda e: self.confirm_canvas.configure(scrollregion=self.confirm_canvas.bbox("all")))
        
        # 변환 버튼 프레임
        convert_frame = ttk.Frame(docs_frame)
        convert_frame.pack(fill="x", pady=10)
        button_container = ttk.Frame(convert_frame)
        button_container.pack(side="right")
        ttk.Button(button_container, text="변환", command=self.convert_selected).pack(side="left", padx=5)
        ttk.Button(button_container, text="전체 변환", command=self.convert_all).pack(side="left")
        
        # 결과 출력 프레임 (전체 높이의 45%)
        result_frame = ttk.LabelFrame(self.root, text="결과", padding=10)
        result_frame.pack(fill="both", expand=True, padx=5, pady=5)
        self.result_text = tk.Text(result_frame, height=40)
        self.result_text.pack(fill="both", expand=True)
    
    def save_paths(self):
        """경로 설정 저장"""
        config = {
            "source_path": self.source_path_var.get(),
            "target_path": self.target_path_var.get()
        }
        try:
            with open(self.config_file, 'w', encoding='utf-8') as f:
                json.dump(config, f, ensure_ascii=False, indent=2)
        except Exception as e:
            self.show_result(f"경로 설정 저장 실패: {str(e)}\n")
    
    def load_paths(self):
        """저장된 경로 설정 불러오기"""
        try:
            if os.path.exists(self.config_file):
                with open(self.config_file, 'r', encoding='utf-8') as f:
                    config = json.load(f)
                    
                source_path = config.get("source_path", "")
                target_path = config.get("target_path", "")
                
                if source_path and os.path.exists(source_path):
                    self.source_path_var.set(source_path)
                    self.load_excel_files(source_path)  # Excel 파일 목록 로드
                    
                if target_path and os.path.exists(target_path):
                    self.target_path_var.set(target_path)
        except Exception as e:
            self.show_result(f"경로 설정 불러오기 실패: {str(e)}\n")
    
    def load_excel_files(self, folder_path):
        """Excel 파일 목록 로드"""
        self.excel_files.clear()
        
        # 기존 체크박스 제거
        for widget in self.select_checkbox_frame.winfo_children():
            widget.destroy()
        
        # 폴더 내 모든 Excel 파일 표시
        row = 0
        for file in sorted(os.listdir(folder_path)):
            if file.endswith('.xlsx'):
                var = tk.BooleanVar()
                self.excel_files[file] = var
                cb = ttk.Checkbutton(self.select_checkbox_frame, text=file, variable=var)
                cb.grid(row=row, column=0, sticky="w", padx=5, pady=2)
                row += 1
        
        # 캔버스 업데이트
        self.select_checkbox_frame.update_idletasks()
        self.select_canvas.configure(scrollregion=self.select_canvas.bbox("all"))
    
    def set_source_path(self):
        """참조 경로 설정"""
        folder_path = filedialog.askdirectory(
            title="Excel 파일이 있는 폴더 선택"
        )
        if folder_path:
            self.source_path_var.set(folder_path)
            self.load_excel_files(folder_path)
            self.save_paths()  # 경로 설정 저장
    
    def set_target_path(self):
        """저장 경로 설정"""
        folder_path = filedialog.askdirectory(
            title="JSON 파일을 저장할 폴더 선택"
        )
        if folder_path:
            self.target_path_var.set(folder_path)
            self.save_paths()  # 경로 설정 저장
    
    def add_to_confirmed(self):
        """선택된 파일을 확정 리스트로 이동"""
        # 기존 확정 리스트의 체크박스 상태 저장
        existing_states = {file: var.get() for file, var in self.confirmed_files.items()}
        
        # 선택된 파일 추가
        for file, var in self.excel_files.items():
            if var.get():
                if file not in self.confirmed_files:
                    # 새로운 파일 추가
                    self.confirmed_files[file] = tk.BooleanVar()
                var.set(False)  # 선택 리스트의 체크박스 해제
        
        # 확정 리스트 업데이트
        self.update_confirmed_list(existing_states)
    
    def update_confirmed_list(self, existing_states=None):
        """확정 리스트 UI 업데이트"""
        # 기존 체크박스 제거
        for widget in self.confirm_checkbox_frame.winfo_children():
            widget.destroy()
        
        # 확정 리스트 재구성
        row = 0
        for file in sorted(self.confirmed_files.keys()):
            var = self.confirmed_files[file]
            if existing_states and file in existing_states:
                var.set(existing_states[file])
            cb = ttk.Checkbutton(self.confirm_checkbox_frame, text=file, variable=var)
            cb.grid(row=row, column=0, sticky="w", padx=5, pady=2)
            row += 1
        
        # 캔버스 업데이트
        self.confirm_checkbox_frame.update_idletasks()
        self.confirm_canvas.configure(scrollregion=self.confirm_canvas.bbox("all"))
    
    def remove_from_confirmed(self):
        """확정 리스트에서 제거"""
        # 체크된 항목 찾기
        to_remove = []
        for file, var in self.confirmed_files.items():
            if var.get():
                to_remove.append(file)
        
        # 체크된 항목 제거
        for file in to_remove:
            del self.confirmed_files[file]
        
        # 확정 리스트 업데이트
        self.update_confirmed_list()
    
    def convert_selected(self):
        """확정 리스트의 파일 변환"""
        files = list(self.confirmed_files.keys())
        if not files:
            self.show_result("변환할 파일이 없습니다.")
            return
        
        self.process_files(files)
    
    def convert_all(self):
        """선택 리스트의 모든 파일 변환"""
        files = list(self.excel_files.keys())
        if not files:
            self.show_result("변환할 파일이 없습니다.")
            return
        
        self.process_files(files)
    
    def process_files(self, files):
        """파일 처리 및 결과 표시"""
        try:
            self.excel_processor.clear_results()
            source_dir = self.source_path_var.get()
            target_dir = self.target_path_var.get()
            
            if not source_dir or not target_dir:
                self.show_result("참조 경로와 저장 경로를 모두 설정해주세요.")
                return
                
            if not os.path.exists(target_dir):
                os.makedirs(target_dir)
                
            self.show_result("변환 시작...\n")
            
            success_files = []
            failed_files = []
            
            for file in files:
                try:
                    excel_path = os.path.join(source_dir, file)
                    json_path = os.path.join(target_dir, os.path.splitext(file)[0] + ".json")
                    
                    self.show_result(f"\n처리 중: {file}\n")
                    self.show_result(f"Excel 경로: {excel_path}\n")
                    self.show_result(f"JSON 경로: {json_path}\n")
                    
                    if not os.path.exists(excel_path):
                        self.show_result(f"오류: Excel 파일을 찾을 수 없습니다 - {excel_path}\n")
                        failed_files.append(file)
                        continue
                        
                    self.excel_processor.set_paths(excel_path, json_path)
                    
                    try:
                        self.excel_processor.load_type_definitions()
                        sheets = self.excel_processor.type_definitions.keys()
                        self.show_result(f"시트 목록: {', '.join(sheets)}\n")
                        
                        sheets_processed = True
                        for sheet in sheets:
                            try:
                                self.show_result(f"시트 처리 중: {sheet}\n")
                                self.excel_processor.process_sheet(sheet)
                            except Exception as e:
                                self.show_result(f"시트 '{sheet}' 처리 중 오류 발생: {str(e)}\n")
                                sheets_processed = False
                                break
                        
                        if sheets_processed:
                            self.show_result("JSON 파일 저장 중...\n")
                            self.excel_processor.save_to_json()
                            self.show_result(f"저장 완료: {json_path}\n")
                            success_files.append(file)
                            self.excel_processor.add_success_file(file)  # 성공한 파일 추가
                        else:
                            failed_files.append(file)
                            
                    except Exception as e:
                        self.show_result(f"파일 처리 중 오류 발생: {str(e)}\n")
                        failed_files.append(file)
                        
                except Exception as e:
                    self.show_result(f"{file} 처리 실패: {str(e)}\n")
                    failed_files.append(file)
            
            self.show_result("\n=== 전체 처리 결과 ===\n")
            self.show_result(f"성공한 파일: {len(success_files)}개\n")
            if success_files:
                self.show_result(f"- {', '.join(success_files)}\n")
            
            self.show_result(f"\n실패한 파일: {len(failed_files)}개\n")
            if failed_files:
                self.show_result(f"- {', '.join(failed_files)}\n")
            
            self.show_result("\n=== 상세 처리 결과 ===\n")
            self.show_processing_results()
            
        except Exception as e:
            self.show_result(f"변환 실패: {str(e)}\n")
    
    def show_processing_results(self):
        """처리 결과 표시"""
        results = self.excel_processor.get_processing_results()
        
        output = "[변환 완료]\n"
        if results.get('success_files'):  # 성공한 파일 목록 표시
            output += f"- 처리된 파일: {', '.join(results['success_files'])}\n"
        if results.get('processed_sheets'):  # 처리된 시트 목록 표시
            output += f"- 처리된 시트: {', '.join(results['processed_sheets'])}\n"
        output += "\n"
        
        if results['null_cells']:
            output += "[Null 처리]\n"
            for null_cell in results['null_cells']:
                output += f"- {null_cell}\n"
            output += "\n"
        
        if results['type_errors']:
            output += "[타입 에러]\n"
            for type_error in results['type_errors']:
                output += f"- {type_error}\n"
        
        self.show_result(output)
    
    def show_result(self, message):
        """결과창에 메시지 표시"""
        self.result_text.delete(1.0, tk.END)
        self.result_text.insert(tk.END, message) 