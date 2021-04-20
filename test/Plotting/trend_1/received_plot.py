import pandas as pd
import matplotlib.pyplot as plt

cluster = 3
df1 = pd.read_csv(str(cluster)+'/TcpNode2.txt')

def get_t_send(point_value):
    # print(df1[df1.id == point_value].send)
    return float(df1[df1.id == point_value].send)

df = pd.read_csv(str(cluster)+'/receiver_log.txt')[-300000:].sort_values(by='point_value').reset_index(drop=True)

# T0 = df.iloc[0].time_received

new = {'delta':[], 'id':[]}
V0 = df.iloc[0].point_value
for i in range(0, len(df)):
    R0 = df.iloc[i]
    try:
        
        T0 = get_t_send(R0.point_value)
        TT = float(R0.time_received)-T0
    except Exception as e:
        print(e)
        continue
    
    if (TT > 4500000): continue
    new['delta'].append(TT/1000)
    new['id'].append(R0.point_value - V0)


pd.DataFrame.from_dict(new).to_csv(str(cluster)+'_tt.csv', index=False)

# sct = plt.scatter(range(len(delta)), delta)
# sct.set_rasterized(True)

# plt.show()