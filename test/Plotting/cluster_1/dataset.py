import pandas as pd
import sys
import matplotlib.pyplot as plt

cluster = 4
fn = 'receiver_log.txt'
node = 'TcpNode'


class recursionlimit:
    def __init__(self, limit):
        self.limit = limit
        self.old_limit = sys.getrecursionlimit()

    def __enter__(self):
        sys.setrecursionlimit(self.limit)

    def __exit__(self, type, value, tb):
        sys.setrecursionlimit(self.old_limit)

def find_row(node, index, df:pd.DataFrame, recursed=0):

    row = df.iloc[index]

    if (row.node != node):
        row, index, recursed = find_row(node, index-1, df, recursed+1)

    return row, index, recursed


# for j in ["5", "4_delay", "4_1", "3_1", "4_v", "3_v", "5_1", "5_delay"]:#[2, 3, 4]:"5", "4_delay", "4_1", "3_1", "4_v", "3_v", "5_1", 
#     df = pd.read_csv(str(j)+'/'+fn)

#     total_time_taken_df = {'batch':[], 'received':[], 'consumed':[], 'completed':[]}

#     for i in range(0, 21, 5):
#         if i == 0: i+=1
#         # Getting the part of the DF and sorting it by time received
#         df_i = df[df.points == i].sort_values(by='time_received').reset_index(drop=True)[500:-500]

#         delays = []
#         for j in range(0, len(df_i)):
#             r1 = df_i.iloc[j]
#             r0 = df_i.iloc[j-1]
#             delay = (r1.time_received - r0.time_received)/1000
#             delays.append(delay)
        
#         print(sum(delays)/len(delays))

        # # Extracting first row
        # row_0 = df_i.iloc[0]
        # with recursionlimit(10000):
        #     row_N, _index, recursed = find_row(row_0.node, len(df_i)-1, df_i)

    # pd.DataFrame.from_dict(total_time_taken_df).to_csv(str(j)+'_totals.csv')

for j in ["5", "4_delay", "4_1", "3_1", "4_v", "3_v", "5_1", "5_delay"]:#[2, 3, 4]:"5", "4_delay", "4_1", "3_1", "4_v", "3_v", "5_1", 
    df = pd.read_csv(str(j)+'/'+fn)

    total_time_taken_df = {'batch':[], 'received':[], 'consumed':[], 'completed':[]}

    for i in range(0, 21, 5):
        if i == 0: i+=1
        
        # Getting the part of the DF and sorting it by time received
        df_i = df[df.points == i].sort_values(by='node_received').reset_index(drop=True)[500:-500]
        

        # Extracting first row
        row_0 = df_i.iloc[0]

        with recursionlimit(10000):
            row_N, _index, recursed = find_row(row_0.node, len(df_i)-1, df_i)

        print(j, i, _index, row_0.node, row_N.node)

        l = int((len(df_i)-recursed)/i)
        total_time_taken_df['batch'].append(row_0.points)
        total_time_taken_df['received'].append (((row_N.time_received-row_0.time_received)  /1000)/l)
        total_time_taken_df['consumed'].append (((row_N.node_received-row_0.node_received)  /1000)/l)
        total_time_taken_df['completed'].append(((row_N.node_completed-row_0.node_completed)/1000)/l)

    pd.DataFrame.from_dict(total_time_taken_df).to_csv(str(j)+'_totals.csv')






