import * as React from 'react';
import styled from 'styled-components';
import { ButtonGroup, Button, FormGroup, Input, Label } from 'reactstrap';
import { MapTileType } from '../maps/IMap';

interface Props {
    mode: string | null;

    isDark: boolean;
    onIsDarkChanged: (isDark: boolean) => void;

    dimension: "2D" | "3D";
    onDimensionChanged: (dimension: "2D" | "3D") => void;

    tileType: MapTileType;
    onTileTypeChanged: (tileType: MapTileType) => void;
}

export default (props: Props) => {
    const [expand, setExpand] = React.useState<boolean>(false);

    const handleIsDark = () => {
        props.onIsDarkChanged(!props.isDark);
    }

    const handleMap2D = () => props.onDimensionChanged("2D");
    const handleMap3D = () => props.onDimensionChanged("3D");
    const handleOpenStreetMap = () => props.onTileTypeChanged(MapTileType.OpenStreetMap);
    const handleOpenTopoMap = () => props.onTileTypeChanged(MapTileType.OpenTopoMap);
    const handleEsriWorldImagery = () => props.onTileTypeChanged(MapTileType.EsriWorldImagery);
    const handleEsriTopo = () => props.onTileTypeChanged(MapTileType.EsriTopo);
    const handleUsVfrSectional = () => props.onTileTypeChanged(MapTileType.UsVfrSectional);
    const handleToggle = () => setExpand(!expand);

    return <>
        {!expand ?
            <CollapsedDiv onClick={handleToggle}>
                <svg xmlns="http://www.w3.org/2000/svg" height="40" width="40" viewBox="0 0 144 144">
                    <g>
                        <path id="path1" transform="rotate(0,72,72) translate(32,38.875) scale(2.5,2.5)  " fill="#000000" d="M18.100006,22.199997C18.5,22.199997,21,23.100006,21,23.100006L21.100006,26.5 18.100006,25.399994z M27.800003,20.600006L27.800003,24.199997 22.400009,26.199997 22.400009,22.399994z M3.9000092,20.5L5.6999969,24 0.1000061,26.5 0.1000061,25.5z M10.800003,20.199997L17.100006,21.899994 17.100006,25C17.100006,24.800003,10.800003,22.699997,10.800003,22.699997z M31.900009,18.800003L31.900009,22.5 28.699997,23.899994 28.699997,20.199997z M18.300003,14.699997C19,14.699997,21,15.100006,21,15.100006L21,21.899994 18.199997,21z M9.6000061,14.100006L9.6999969,22.699997 6.9000092,23.600006 4.8000031,19.600006z M11,13.300003C14.300003,13.600006,17.100006,14.399994,17.100006,14.399994L17.100006,20.600006 11,19.100006z M27.800003,11.899994L27.800003,19.5 22.5,21.399994 22.5,14.5C22.5,14.399994,27.100006,12.100006,27.800003,11.899994z M9.6999969,10.600006L9.6999969,12.399994C9.6999969,12.399994,5.1000061,17.199997,4.1000061,18.600006L0.1000061,23.199997 0.1000061,19.5C0.1000061,19.699997,8.1000061,10.899994,9.6999969,10.600006z M10.900009,10.5C11.199997,10.699997,21.100006,12.399994,21.100006,12.399994L21.100006,14C20.5,13.800003,10.900009,12.199997,10.900009,12.199997z M31.900009,9.6999969L31.900009,17.5 28.699997,18.699997 28.699997,11.300003z M32,4.6000061L32,8.3999939 22.300003,13.300003 22.300003,11.899994C22.300003,11.899994,29.100006,7.6999969,32,4.6000061z M9.6999969,0.3999939L9.6999969,8.1000061C5,10.5,0,16.199997,0,16.199997L0,4z M10.900009,0.19999695L21.199997,4 21.199997,9.8000031 10.900009,8.1000061z M31.900009,0L31.900009,1.6000061C31.900009,1.6000061,25.699997,7.6999969,22.300003,9.1000061L22.300003,3.6999969z" />
                    </g>
                </svg>
            </CollapsedDiv> :
            <ExpandedDiv>
                <CollapsedButton className="btn" onClick={handleToggle}><i className={"fas " + (expand ? "fa-chevron-up" : "fa-chevron-down")}></i></CollapsedButton>
                <StyledFormGroup check>
                    <Input type="checkbox" name="checkIsDark" id="checkIsDark" checked={props.isDark} onChange={handleIsDark} />
                    <Label for="checkIsDark" check>Dark Mode</Label>
                </StyledFormGroup>
                {props.mode !== "MSFS" && <TypeWrapper className="btn-group-vertical">
                    <ButtonGroup>
                        <Button className="btn btn-light" active={props.dimension === "2D"} onClick={handleMap2D}>2D</Button>
                        <Button className="btn btn-light" active={props.dimension === "3D"} onClick={handleMap3D}>3D</Button>
                    </ButtonGroup>
                </TypeWrapper>}
                <LayerWrapper className="btn-group-vertical">
                    <Button className="btn btn-light" active={props.tileType === MapTileType.OpenStreetMap} onClick={handleOpenStreetMap}>OpenStreetMap</Button>
                    <Button className="btn btn-light" active={props.tileType === MapTileType.OpenTopoMap} onClick={handleOpenTopoMap}>OpenTopoMap</Button>
                    <Button className="btn btn-light" active={props.tileType === MapTileType.EsriWorldImagery} onClick={handleEsriWorldImagery}>Esri Imagery</Button>
                    <Button className="btn btn-light" active={props.tileType === MapTileType.EsriTopo} onClick={handleEsriTopo}>Esri Topo</Button>
                    <Button className="btn btn-light" active={props.tileType === MapTileType.UsVfrSectional} onClick={handleUsVfrSectional}>US VFR</Button>
                </LayerWrapper>
            </ExpandedDiv>
        }
    </>
}

const CollapsedDiv = styled.div`
position: absolute;
top: 80px;
left: 10px;
z-index: 1000;
width: 40px;
height: 40px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
background-color: rgba(255,255,255,1);
opacity: 0.8;
cursor: pointer;
`

const ExpandedDiv = styled.div`
position: absolute;
top: 80px;
left: 10px;
z-index: 10000;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
background-color: rgba(255,255,255,0.6);
cursor: pointer;
padding: 12px 10px 10px 10px;
`

const CollapsedButton = styled.button`
padding: 0;
font-size: 8px;
display: block;
position: absolute;
left: 0;
right: 0;
top: 0;
width: 100%;
height: 12px;
`

const StyledFormGroup = styled(FormGroup)`
display: block;
background-color: white;
width: 140px;
margin-top: 10px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
padding: 4px 4px 4px 28px;
`

const TypeWrapper = styled.div`
display: block;
margin-top: 10px;
width: 140px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
`;

const LayerWrapper = styled.div`
display: block;
margin-top: 10px;
width: 140px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;

button {
    display: block;
}
`;