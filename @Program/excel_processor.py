import pandas as pd
import json
from typing import Dict, List, Any, Tuple

class ExcelProcessor:
    def __init__(self):
        self.source_path = None
        self.target_path = None
        self.type_definitions = {}
        self.sheets_data = {}
        self.processed_sheets = set()
        self.null_cells = []
        self.type_errors = []
        self.success_files = []  # 성공적으로 처리된 파일 목록
    
    def set_paths(self, source_path: str, target_path: str) -> None:
        """경로 설정"""
        self.source_path = source_path
        self.target_path = target_path
    
    def load_type_definitions(self) -> Dict[str, Dict[str, str]]:
        """TypeDefinition 시트에서 타입 정의 로드"""
        try:
            df = pd.read_excel(self.source_path, sheet_name="TypeDefinition")
            for _, row in df.iterrows():
                sheet_name = row["SheetName"]
                if sheet_name not in self.type_definitions:
                    self.type_definitions[sheet_name] = {}
                self.type_definitions[sheet_name][row["ColumnName"]] = row["Type"]
            return self.type_definitions
        except Exception as e:
            raise Exception(f"TypeDefinition 시트 로드 실패: {str(e)}")
    
    def validate_cell_type(self, value: Any, expected_type: str) -> Tuple[Any, bool]:
        """셀 데이터 타입 검증"""
        try:
            if pd.isna(value):
                return None, True
            
            if expected_type == "INT":
                return int(value), True
            elif expected_type == "FLOAT":
                return float(value), True
            elif expected_type == "STRING":
                return str(value), True
            return value, False
        except:
            return value, False
    
    def process_sheet(self, sheet_name: str) -> None:
        """시트 처리"""
        try:
            df = pd.read_excel(self.source_path, sheet_name=sheet_name)
            sheet_data = []
            
            for row_idx, row in df.iterrows():
                row_data = {}
                for col_name in df.columns:
                    value = row[col_name]
                    expected_type = self.type_definitions[sheet_name].get(col_name, "STRING")
                    
                    processed_value, is_valid = self.validate_cell_type(value, expected_type)
                    
                    if processed_value is None:
                        self.null_cells.append(
                            f"{sheet_name} > {row_idx+2},{col_name}: null 처리됨"
                        )
                    elif not is_valid:
                        self.type_errors.append(
                            f"{sheet_name} > {row_idx+2},{col_name}: {expected_type} 타입 불일치"
                        )
                        processed_value = "Error"
                    
                    row_data[col_name] = processed_value
                
                sheet_data.append(row_data)
            
            self.sheets_data[sheet_name] = sheet_data
            self.processed_sheets.add(sheet_name)
            
        except Exception as e:
            raise Exception(f"{sheet_name} 시트 처리 실패: {str(e)}")
    
    def save_to_json(self) -> None:
        """JSON 파일로 저장"""
        try:
            with open(self.target_path, 'w', encoding='utf-8') as f:
                json.dump(self.sheets_data, f, ensure_ascii=False, indent=2)
        except Exception as e:
            raise Exception(f"JSON 저장 실패: {str(e)}")
    
    def clear_results(self) -> None:
        """결과 초기화"""
        self.sheets_data = {}
        self.processed_sheets = set()
        self.null_cells = []
        self.type_errors = []
        self.success_files = []  # 성공한 파일 목록도 초기화
    
    def add_success_file(self, filename):
        """성공적으로 처리된 파일 추가"""
        if filename not in self.success_files:
            self.success_files.append(filename)
    
    def get_processing_results(self) -> Dict[str, List[str]]:
        """처리 결과 반환"""
        return {
            'processed_sheets': list(self.processed_sheets),
            'null_cells': self.null_cells,
            'type_errors': self.type_errors,
            'success_files': self.success_files  # 성공한 파일 목록 추가
        } 