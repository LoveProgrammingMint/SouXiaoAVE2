#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LightGBM Malware Detection Model Training Script
Trains a binary classifier on PE file features extracted from SQLite database.
"""

import sqlite3
import numpy as np
import lightgbm as lgb
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.metrics import (
    accuracy_score, precision_score, recall_score, f1_score,
    roc_auc_score, confusion_matrix, classification_report
)
import os
import sys
from datetime import datetime

# Configuration
DB_PATH = r"D:\Dataset\IceZero\ZeroflowDataset.db"
MODEL_PATH = r"D:\SouXiaoAVE\AIModel\lightgbm_model.txt"
FEATURE_COUNT = 512

# LightGBM parameters
LGBM_PARAMS = {
    'objective': 'binary',
    'metric': ['auc', 'binary_logloss'],
    'boosting_type': 'gbdt',
    'num_leaves': 63,
    'learning_rate': 0.05,
    'feature_fraction': 0.8,
    'bagging_fraction': 0.8,
    'bagging_freq': 5,
    'verbose': -1,
    'n_jobs': -1,
    'seed': 42,
    'min_data_in_leaf': 20,
    'max_depth': -1,
    'lambda_l1': 0.1,
    'lambda_l2': 0.1,
}

NUM_ROUNDS = 500
EARLY_STOPPING_ROUNDS = 50


def load_data_from_sqlite(db_path):
    """Load features and labels from SQLite database."""
    print(f"[INFO] Loading data from: {db_path}")
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    # Get total count
    cursor.execute("SELECT COUNT(*) FROM samples")
    total_count = cursor.fetchone()[0]
    print(f"[INFO] Total samples in database: {total_count}")
    
    # Get label distribution
    cursor.execute("SELECT label, COUNT(*) FROM samples GROUP BY label")
    label_dist = cursor.fetchall()
    print(f"[INFO] Label distribution: {dict(label_dist)}")
    
    # Load data - only malicious and benign, skip unknown
    cursor.execute("""
        SELECT s.sha256, s.file_name, s.file_size, s.label, 
               f.feature_0, f.feature_1, f.feature_2, f.feature_3, f.feature_4,
               f.feature_5, f.feature_6, f.feature_7, f.feature_8, f.feature_9,
               f.feature_10, f.feature_11, f.feature_12, f.feature_13, f.feature_14,
               f.feature_15, f.feature_16, f.feature_17, f.feature_18, f.feature_19,
               f.feature_20, f.feature_21, f.feature_22, f.feature_23, f.feature_24,
               f.feature_25, f.feature_26, f.feature_27, f.feature_28, f.feature_29,
               f.feature_30, f.feature_31, f.feature_32, f.feature_33, f.feature_34,
               f.feature_35, f.feature_36, f.feature_37, f.feature_38, f.feature_39,
               f.feature_40, f.feature_41, f.feature_42, f.feature_43, f.feature_44,
               f.feature_45, f.feature_46, f.feature_47, f.feature_48, f.feature_49,
               f.feature_50, f.feature_51, f.feature_52, f.feature_53, f.feature_54,
               f.feature_55, f.feature_56, f.feature_57, f.feature_58, f.feature_59,
               f.feature_60, f.feature_61, f.feature_62, f.feature_63, f.feature_64,
               f.feature_65, f.feature_66, f.feature_67, f.feature_68, f.feature_69,
               f.feature_70, f.feature_71, f.feature_72, f.feature_73, f.feature_74,
               f.feature_75, f.feature_76, f.feature_77, f.feature_78, f.feature_79,
               f.feature_80, f.feature_81, f.feature_82, f.feature_83, f.feature_84,
               f.feature_85, f.feature_86, f.feature_87, f.feature_88, f.feature_89,
               f.feature_90, f.feature_91, f.feature_92, f.feature_93, f.feature_94,
               f.feature_95, f.feature_96, f.feature_97, f.feature_98, f.feature_99,
               f.feature_100, f.feature_101, f.feature_102, f.feature_103, f.feature_104,
               f.feature_105, f.feature_106, f.feature_107, f.feature_108, f.feature_109,
               f.feature_110, f.feature_111, f.feature_112, f.feature_113, f.feature_114,
               f.feature_115, f.feature_116, f.feature_117, f.feature_118, f.feature_119,
               f.feature_120, f.feature_121, f.feature_122, f.feature_123, f.feature_124,
               f.feature_125, f.feature_126, f.feature_127, f.feature_128, f.feature_129,
               f.feature_130, f.feature_131, f.feature_132, f.feature_133, f.feature_134,
               f.feature_135, f.feature_136, f.feature_137, f.feature_138, f.feature_139,
               f.feature_140, f.feature_141, f.feature_142, f.feature_143, f.feature_144,
               f.feature_145, f.feature_146, f.feature_147, f.feature_148, f.feature_149,
               f.feature_150, f.feature_151, f.feature_152, f.feature_153, f.feature_154,
               f.feature_155, f.feature_156, f.feature_157, f.feature_158, f.feature_159,
               f.feature_160, f.feature_161, f.feature_162, f.feature_163, f.feature_164,
               f.feature_165, f.feature_166, f.feature_167, f.feature_168, f.feature_169,
               f.feature_170, f.feature_171, f.feature_172, f.feature_173, f.feature_174,
               f.feature_175, f.feature_176, f.feature_177, f.feature_178, f.feature_179,
               f.feature_180, f.feature_181, f.feature_182, f.feature_183, f.feature_184,
               f.feature_185, f.feature_186, f.feature_187, f.feature_188, f.feature_189,
               f.feature_190, f.feature_191, f.feature_192, f.feature_193, f.feature_194,
               f.feature_195, f.feature_196, f.feature_197, f.feature_198, f.feature_199,
               f.feature_200, f.feature_201, f.feature_202, f.feature_203, f.feature_204,
               f.feature_205, f.feature_206, f.feature_207, f.feature_208, f.feature_209,
               f.feature_210, f.feature_211, f.feature_212, f.feature_213, f.feature_214,
               f.feature_215, f.feature_216, f.feature_217, f.feature_218, f.feature_219,
               f.feature_220, f.feature_221, f.feature_222, f.feature_223, f.feature_224,
               f.feature_225, f.feature_226, f.feature_227, f.feature_228, f.feature_229,
               f.feature_230, f.feature_231, f.feature_232, f.feature_233, f.feature_234,
               f.feature_235, f.feature_236, f.feature_237, f.feature_238, f.feature_239,
               f.feature_240, f.feature_241, f.feature_242, f.feature_243, f.feature_244,
               f.feature_245, f.feature_246, f.feature_247, f.feature_248, f.feature_249,
               f.feature_250, f.feature_251, f.feature_252, f.feature_253, f.feature_254,
               f.feature_255, f.feature_256, f.feature_257, f.feature_258, f.feature_259,
               f.feature_260, f.feature_261, f.feature_262, f.feature_263, f.feature_264,
               f.feature_265, f.feature_266, f.feature_267, f.feature_268, f.feature_269,
               f.feature_270, f.feature_271, f.feature_272, f.feature_273, f.feature_274,
               f.feature_275, f.feature_276, f.feature_277, f.feature_278, f.feature_279,
               f.feature_280, f.feature_281, f.feature_282, f.feature_283, f.feature_284,
               f.feature_285, f.feature_286, f.feature_287, f.feature_288, f.feature_289,
               f.feature_290, f.feature_291, f.feature_292, f.feature_293, f.feature_294,
               f.feature_295, f.feature_296, f.feature_297, f.feature_298, f.feature_299,
               f.feature_300, f.feature_301, f.feature_302, f.feature_303, f.feature_304,
               f.feature_305, f.feature_306, f.feature_307, f.feature_308, f.feature_309,
               f.feature_310, f.feature_311, f.feature_312, f.feature_313, f.feature_314,
               f.feature_315, f.feature_316, f.feature_317, f.feature_318, f.feature_319,
               f.feature_320, f.feature_321, f.feature_322, f.feature_323, f.feature_324,
               f.feature_325, f.feature_326, f.feature_327, f.feature_328, f.feature_329,
               f.feature_330, f.feature_331, f.feature_332, f.feature_333, f.feature_334,
               f.feature_335, f.feature_336, f.feature_337, f.feature_338, f.feature_339,
               f.feature_340, f.feature_341, f.feature_342, f.feature_343, f.feature_344,
               f.feature_345, f.feature_346, f.feature_347, f.feature_348, f.feature_349,
               f.feature_350, f.feature_351, f.feature_352, f.feature_353, f.feature_354,
               f.feature_355, f.feature_356, f.feature_357, f.feature_358, f.feature_359,
               f.feature_360, f.feature_361, f.feature_362, f.feature_363, f.feature_364,
               f.feature_365, f.feature_366, f.feature_367, f.feature_368, f.feature_369,
               f.feature_370, f.feature_371, f.feature_372, f.feature_373, f.feature_374,
               f.feature_375, f.feature_376, f.feature_377, f.feature_378, f.feature_379,
               f.feature_380, f.feature_381, f.feature_382, f.feature_383, f.feature_384,
               f.feature_385, f.feature_386, f.feature_387, f.feature_388, f.feature_389,
               f.feature_390, f.feature_391, f.feature_392, f.feature_393, f.feature_394,
               f.feature_395, f.feature_396, f.feature_397, f.feature_398, f.feature_399,
               f.feature_400, f.feature_401, f.feature_402, f.feature_403, f.feature_404,
               f.feature_405, f.feature_406, f.feature_407, f.feature_408, f.feature_409,
               f.feature_410, f.feature_411, f.feature_412, f.feature_413, f.feature_414,
               f.feature_415, f.feature_416, f.feature_417, f.feature_418, f.feature_419,
               f.feature_420, f.feature_421, f.feature_422, f.feature_423, f.feature_424,
               f.feature_425, f.feature_426, f.feature_427, f.feature_428, f.feature_429,
               f.feature_430, f.feature_431, f.feature_432, f.feature_433, f.feature_434,
               f.feature_435, f.feature_436, f.feature_437, f.feature_438, f.feature_439,
               f.feature_440, f.feature_441, f.feature_442, f.feature_443, f.feature_444,
               f.feature_445, f.feature_446, f.feature_447, f.feature_448, f.feature_449,
               f.feature_450, f.feature_451, f.feature_452, f.feature_453, f.feature_454,
               f.feature_455, f.feature_456, f.feature_457, f.feature_458, f.feature_459,
               f.feature_460, f.feature_461, f.feature_462, f.feature_463, f.feature_464,
               f.feature_465, f.feature_466, f.feature_467, f.feature_468, f.feature_469,
               f.feature_470, f.feature_471, f.feature_472, f.feature_473, f.feature_474,
               f.feature_475, f.feature_476, f.feature_477, f.feature_478, f.feature_479,
               f.feature_480, f.feature_481, f.feature_482, f.feature_483, f.feature_484,
               f.feature_485, f.feature_486, f.feature_487, f.feature_488, f.feature_489,
               f.feature_490, f.feature_491, f.feature_492, f.feature_493, f.feature_494,
               f.feature_495, f.feature_496, f.feature_497, f.feature_498, f.feature_499,
               f.feature_500, f.feature_501, f.feature_502, f.feature_503, f.feature_504,
               f.feature_505, f.feature_506, f.feature_507, f.feature_508, f.feature_509,
               f.feature_510, f.feature_511
        FROM samples s
        JOIN features f ON s.id = f.sample_id
        WHERE s.label IN ('malicious', 'benign')
    """)
    
    rows = cursor.fetchall()
    conn.close()
    
    print(f"[INFO] Loaded {len(rows)} samples (malicious + benign)")
    
    if len(rows) == 0:
        raise ValueError("No data found in database!")
    
    # Parse data
    X = []
    y = []
    sha256_list = []
    
    for row in rows:
        sha256_list.append(row[0])
        # Features start from index 4
        features = list(row[4:])
        X.append(features)
        # Label: malicious=1, benign=0
        label = 1 if row[3] == 'malicious' else 0
        y.append(label)
    
    X = np.array(X, dtype=np.float32)
    y = np.array(y, dtype=np.int32)
    
    print(f"[INFO] Feature matrix shape: {X.shape}")
    print(f"[INFO] Label distribution: malicious={np.sum(y)}, benign={len(y) - np.sum(y)}")
    
    return X, y, sha256_list


def train_model(X_train, y_train, X_val, y_val):
    """Train LightGBM model."""
    print("\n[INFO] Training LightGBM model...")
    print(f"[INFO] Training samples: {len(X_train)}")
    print(f"[INFO] Validation samples: {len(X_val)}")
    
    train_data = lgb.Dataset(X_train, label=y_train)
    val_data = lgb.Dataset(X_val, label=y_val, reference=train_data)
    
    model = lgb.train(
        LGBM_PARAMS,
        train_data,
        num_boost_round=NUM_ROUNDS,
        valid_sets=[train_data, val_data],
        valid_names=['train', 'valid'],
        callbacks=[
            lgb.early_stopping(stopping_rounds=EARLY_STOPPING_ROUNDS, verbose=True),
            lgb.log_evaluation(period=50)
        ]
    )
    
    print(f"\n[INFO] Best iteration: {model.best_iteration}")
    print(f"[INFO] Best score: {model.best_score}")
    
    return model


def evaluate_model(model, X_test, y_test):
    """Evaluate model performance."""
    print("\n[INFO] Evaluating model on test set...")
    
    y_pred_proba = model.predict(X_test, num_iteration=model.best_iteration)
    y_pred = (y_pred_proba >= 0.5).astype(int)
    
    # Calculate metrics
    accuracy = accuracy_score(y_test, y_pred)
    precision = precision_score(y_test, y_pred)
    recall = recall_score(y_test, y_pred)
    f1 = f1_score(y_test, y_pred)
    auc = roc_auc_score(y_test, y_pred_proba)
    
    print(f"\n=== Model Performance ===")
    print(f"Accuracy:  {accuracy:.4f}")
    print(f"Precision: {precision:.4f}")
    print(f"Recall:    {recall:.4f}")
    print(f"F1 Score:  {f1:.4f}")
    print(f"AUC-ROC:   {auc:.4f}")
    
    # Confusion matrix
    cm = confusion_matrix(y_test, y_pred)
    print(f"\nConfusion Matrix:")
    print(f"  TN={cm[0,0]}, FP={cm[0,1]}")
    print(f"  FN={cm[1,0]}, TP={cm[1,1]}")
    
    # Classification report
    print(f"\nClassification Report:")
    print(classification_report(y_test, y_pred, target_names=['Benign', 'Malicious']))
    
    return {
        'accuracy': accuracy,
        'precision': precision,
        'recall': recall,
        'f1': f1,
        'auc': auc
    }


def save_model(model, model_path):
    """Save model to file."""
    print(f"\n[INFO] Saving model to: {model_path}")
    
    # Ensure directory exists
    os.makedirs(os.path.dirname(model_path), exist_ok=True)
    
    # Save model in text format (compatible with C API)
    model.save_model(model_path)
    
    # Also save as JSON for backup
    json_path = model_path.replace('.txt', '.json')
    model.save_model(json_path)
    
    print(f"[INFO] Model saved successfully!")
    print(f"[INFO] Text model: {model_path}")
    print(f"[INFO] JSON model: {json_path}")


def print_feature_importance(model, top_n=20):
    """Print feature importance."""
    print(f"\n=== Top {top_n} Feature Importance ===")
    
    importance = model.feature_importance(importance_type='gain')
    feature_names = [f'feature_{i}' for i in range(len(importance))]
    
    # Sort by importance
    indices = np.argsort(importance)[::-1]
    
    for i in range(min(top_n, len(indices))):
        idx = indices[i]
        print(f"  {feature_names[idx]}: {importance[idx]:.2f}")


def main():
    print("=" * 60)
    print("LightGBM Malware Detection Model Training")
    print(f"Started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    # Load data
    X, y, sha256_list = load_data_from_sqlite(DB_PATH)
    
    # Check for NaN values
    nan_count = np.isnan(X).sum()
    if nan_count > 0:
        print(f"[WARN] Found {nan_count} NaN values, replacing with 0")
        X = np.nan_to_num(X, nan=0.0)
    
    # Split data
    print("\n[INFO] Splitting data...")
    X_train, X_temp, y_train, y_temp = train_test_split(
        X, y, test_size=0.3, random_state=42, stratify=y
    )
    X_val, X_test, y_val, y_test = train_test_split(
        X_temp, y_temp, test_size=0.5, random_state=42, stratify=y_temp
    )
    
    print(f"[INFO] Train: {len(X_train)} ({np.sum(y_train)} malicious)")
    print(f"[INFO] Val:   {len(X_val)} ({np.sum(y_val)} malicious)")
    print(f"[INFO] Test:  {len(X_test)} ({np.sum(y_test)} malicious)")
    
    # Train model
    model = train_model(X_train, y_train, X_val, y_val)
    
    # Evaluate model
    metrics = evaluate_model(model, X_test, y_test)
    
    # Print feature importance
    print_feature_importance(model)
    
    # Save model
    save_model(model, MODEL_PATH)
    
    print("\n" + "=" * 60)
    print("Training completed successfully!")
    print(f"Finished at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    return model, metrics


if __name__ == "__main__":
    try:
        model, metrics = main()
    except Exception as e:
        print(f"\n[FATAL] Training failed: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
