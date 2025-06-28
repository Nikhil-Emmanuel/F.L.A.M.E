import sys
import pickle
import numpy as np

with open('weld_model.pkl', 'rb') as f:
    model = pickle.load(f)

temperature = float(sys.argv[1])
input_array = np.array([[temperature]])
predicted = model.predict(input_array)

# Clamp to non-negative and round to integer
remaining_welds = max(0, int(round(predicted[0])))
print(remaining_welds)
