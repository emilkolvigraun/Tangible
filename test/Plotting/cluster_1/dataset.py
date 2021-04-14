import pandas as pd


cluster = 4
fn = 'receiver_log.txt'
node = 'TcpNode'

for j in [2, 3, 4]:
    df = pd.read_csv(str(j)+'/'+fn)

    total_time_taken_df = {'batch':[], 'received':[], 'consumed':[], 'completed':[]}

    for i in range(0, 21, 5):
        if i == 0: i+=1
        
        # Getting the part of the DF and sorting it by time received
        df_i = df[df.points == i].sort_values(by='node_received').reset_index(drop=True)
        
        # Extracting first row
        row_0 = df_i.iloc[0]
        row_N = df_i.iloc[-1]
        total_time_taken_df['batch'].append(row_0.points)
        total_time_taken_df['received'].append (((row_N.time_received-row_0.time_received)  /1000)/30000)
        total_time_taken_df['consumed'].append (((row_N.node_received-row_0.node_received)  /1000)/30000)
        total_time_taken_df['completed'].append(((row_N.node_completed-row_0.node_completed)/1000)/30000)

    pd.DataFrame.from_dict(total_time_taken_df).to_csv(str(j)+'_totals.csv')





