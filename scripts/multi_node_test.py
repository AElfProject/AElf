#!/usr/bin/env python3
# _*_ coding:utf-8 _*_
# Author:Liuyoushui
# Time = 2019/3/1 15:48

import  os
import requests
import json

_BASE_URL = "http://%s:%s/%s/"
_HEADERS = {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
}

_BLOCK_INFO_PLAYLOAD = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "GetBlockInfo",
    "params": {
        "blockHeight": "1",
        "includeTransactions": True
    }
}

_BLOCK_HEIGHT_PAYLOAD = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "GetBlockHeight",
    "params": {
    }
}


def node_start():
    print('1:Download the key........')
    os.system("mkdir -p ~/.local/share/aelf/keys")
    os.system("cd ~/.local/share/aelf/keys && wget https://config-1257167156.cos.ap-chengdu.myqcloud.com/ELF_3DyQHandGuiirB5Co4Kagvr8cu6npfnFVeKbSpKhm1kqVht.ak")
    os.system("cd ~/.local/share/aelf/keys && wget https://config-1257167156.cos.ap-chengdu.myqcloud.com/ELF_3GM5SZPSnyTeDHQngudoFzU89JBevrfeRxBDDurkZdZNssi.ak")
    os.system("cd ~/.local/share/aelf/keys && wget https://config-1257167156.cos.ap-chengdu.myqcloud.com/ELF_Vhr4pHiaJTPeSFHM8NaUaJRsdaypcQ86CkgAgcHZkX41Zi.ak")


    print('2:Download configuration file............')
    os.system("mkdir /tmp/aelf1 && cd /tmp/aelf1 && wget https://config-1257167156.cos.ap-chengdu.myqcloud.com/aelf1/appsettings.json")
    os.system("mkdir /tmp/aelf2 && cd /tmp/aelf2 && wget https://config-1257167156.cos.ap-chengdu.myqcloud.com/aelf2/appsettings.json")
    os.system("mkdir /tmp/aelf3 && cd /tmp/aelf3 && wget https://config-1257167156.cos.ap-chengdu.myqcloud.com/aelf3/appsettings.json")


    print('3:Download the source code...........')
    os.system("cd /tmp && git clone  https://github.com/AElfProject/AElf.git /tmp/aelf-git")
    print('4:Compile the source code...........')
    os.system("cd /tmp/aelf-git && dotnet publish  -o /tmp/aelf-node")
    print('5:killall dotnet redis-cli flushall........')
    os.system("killall dotnet")
    os.system("redis-cli flushall")
    print('Start node 1')
    os.system("cd /tmp/aelf1 && nohup dotnet /tmp/aelf-node/AElf.Launcher.dll  > /tmp/myout1.file 2>&1 &")
    os.system("sleep 20")
    print('Start node 2')
    os.system("cd /tmp/aelf2 && nohup dotnet /tmp/aelf-node/AElf.Launcher.dll > /tmp/myout2.file 2>&1 &")
    os.system("sleep 20")
    print('Start node 3')
    os.system("cd /tmp/aelf3 && nohup dotnet /tmp/aelf-node/AElf.Launcher.dll  > /tmp/myout3.file 2>&1 &")
    os.system("sleep 300")
    os.system("netstat -lntp")
    print('--------------------------------------------------------------')

# Check block height
# Check block hash
def _do_request(url, payload):
    """Get info
    """
    response = requests.post(url, json=payload, headers=_HEADERS)
    return response.json()


def main():
    # node_start
    try:
        node_start()
    except Exception as e:
        print('error',e)
        exit(100)
    """Main process"""
    # Get block height
    try:
        chain1 = _do_request(_BASE_URL % ('127.0.0.1', 8000, 'chain'), _BLOCK_HEIGHT_PAYLOAD)
        block_height = chain1['result']

        print('Start check....\n >> block height: %s' % block_height)
        for i in range(2, block_height):
            _BLOCK_INFO_PLAYLOAD['params']['blockHeight'] = str(i)
            chain1 = _do_request(_BASE_URL % ('127.0.0.1', 8000, 'chain'), _BLOCK_INFO_PLAYLOAD)
            chain2 = _do_request(_BASE_URL % ('127.0.0.1', 8001, 'chain'), _BLOCK_INFO_PLAYLOAD)
            chain3 = _do_request(_BASE_URL % ('127.0.0.1', 8002, 'chain'), _BLOCK_INFO_PLAYLOAD)
            chain1_block_hash = chain1['result'].get('BlockHash', '')
            chain2_block_hash = chain2['result'].get('BlockHash', '')
            chain3_block_hash = chain3['result'].get('BlockHash', '')
            if (chain1_block_hash == chain2_block_hash == chain3_block_hash) and chain1_block_hash != '':
                print(i,'--- ok --->',chain1_block_hash,chain2_block_hash,chain3_block_hash)
                continue

            print("Missing match block hash at %s: %s, %s, %s" %
                  (i, chain1_block_hash, chain2_block_hash, chain3_block_hash))
            os.system("killall dotnet")
            exit(101)
    except Exception as e:
        print('error:',e)
        exit(102)

    print('Done.')


if __name__ == '__main__':
    main()
