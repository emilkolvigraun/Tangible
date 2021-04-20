import pandas as pd

def get_data(df):
    return df[df.extra=='append'],df[df.extra=='driver'],df[df.extra=='completed'].reset_index(drop=True)

for c in [3]:#, 4]:

    if c == 3: 
        points=[1,10,15,20,21,22,23,24,25,26]
    else: 
        points=[1]

    for p in points:
        
        if p==1 and c==3: 
            nodes=[2,4]
        elif p==10 and c==3: 
            nodes=[2,4]
        elif p==15 and c==3: 
            nodes=[0,4]
        elif p==20 and c==3: 
            nodes=[0,2]
        elif p==21 and c==3: 
            nodes=[0,3]
        elif p==22 and c==3: 
            nodes=[2,3]
        elif p==23 and c==3: 
            nodes=[0,2]
        elif p==24 and c==3: 
            nodes=[0,3]
        elif p==25 and c==3: 
            nodes=[0,2]
        elif p==26 and c==3: 
            nodes=[0,3]
        # elif p==1 and c==4:
        #     nodes=[0,2,4]

        
        data_dct = {'ptime':[], 'atime':[], 'id':[], 'time':[], 'points':[], 'node':[]}
        # data_dct = {'delta_idle':[], 'time_proc':[], 'delta_exec':[], 'total':[], 'id':[], 'time':[], 'nodes':[]}
        
        for n in nodes:
            df = pd.read_csv(str(c)+'/'+str(p)+'/TcpNode'+str(n)+'.txt')
            _append, _driver, _complete = get_data(df)
            
            print(c, p, n, len(_complete))  
            
            # T0 = _complete.iloc[0].receive
            # offset = 0
            for i in range(len(_complete)):
                c_row = _complete.iloc[i]
                _id = c_row.id

                try:
                    int(_id)
                except:
                    continue

                apnd = _append[_append.id == _id]
                driv = _driver[_driver.id == _id]

                if(len(apnd)<1 or len(driv)<1 or len(apnd)>1 or len(driv)>1): continue

                data_dct['points'].append(n)
                data_dct['ptime'].append(float(c_row.receive)-float(driv.receive))
                data_dct['atime'].append(float(c_row.receive)-float(apnd.receive))
                data_dct['id'].append(int(_id))
                data_dct['time'].append(float(c_row.receive))
                data_dct['node'].append(c_row.node)



                # data_dct['total'].append(float(c_row.receive)-float(apnd.send))
                # data_dct['time_proc'].append(float(c_row.receive)-float(driv.receive))
                # data_dct['delta_idle'].append(float(driv.receive)-float(apnd.receive))
            # offset = _complete.
        pd.DataFrame.from_dict(data_dct).sort_values(by='id').reset_index(drop=True).to_csv(str(c)+'_'+str(p)+'.csv')












# df = pd.read_csv('3_1.csv').sort_values(by='id')
# df.to_csv('3_1.csv')